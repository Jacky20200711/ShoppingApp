using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ShoppingApp.Data;
using ShoppingApp.Models;
using X.PagedList;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

namespace ShoppingApp.Controllers
{
    [Authorize]
    public class OrderFormController : Controller
    {
        // 每個分頁最多顯示10筆
        private readonly int pageSize = 10;

        // 注入會用到的工具
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IMemoryCache _memoryCache;

        public OrderFormController(
                ApplicationDbContext context, 
                ILogger<OrderFormController> logger,
                UserManager<IdentityUser> userManager,
                IMemoryCache memoryCache)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _memoryCache = memoryCache;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name))
            {
                // 返回該 UserId 所下的訂單，並按照日期排序(新->舊)
                return View(await _context.OrderForm.Where(o => o.SenderEmail == User.Identity.Name).OrderByDescending(o => o.CreateTime).ToPagedListAsync(page, pageSize));
            }
            else
            {
                // 如果是管理員，則返回所有人的訂單
                return View(await _context.OrderForm.OrderByDescending(o => o.CreateTime).ToPagedListAsync(page, pageSize));
            }
        }

        public async Task<IActionResult> Details(int? id, int returnPage = 0)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (returnPage != 0)
            {
                HttpContext.Session.SetInt32("returnPage", returnPage);
            }

            var orderForm = await _context.OrderForm
                .FirstOrDefaultAsync(m => m.Id == id);
            if (orderForm == null)
            {
                return NotFound();
            }

            // 如果不是管理員，則只能查看自己的訂單明細
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name))
            {
                if (orderForm.SenderEmail != User.Identity.Name) return NotFound(); 
            }

            return View(await _context.OrderDetail.Where(o => o.OrderId == id).ToListAsync());
        }

        public IActionResult Create(int returnPage = 0)
        {
            if (returnPage != 0)
            {
                HttpContext.Session.SetInt32("returnPage", returnPage);
            }

            return View();
        }
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ReceiverName,ReceiverPhone,ReceiverAddress")] OrderForm orderForm)
        {
            var currentCart = CartManager.GetCurrentCart();

            if (ModelState.IsValid)
            {
                List<string> QuantityError = new List<string>();

                // 檢查購物車是否為空
                if (currentCart.Count() < 1)
                {
                    QuantityError.Add($"您的購物車內沒有任何東西!");
                    ViewBag.QuantityError = QuantityError;
                    return View();
                }

                // 檢查庫存
                foreach (var p in currentCart)
                {
                    Product product = await _context.Product.FirstOrDefaultAsync(m => m.Id == p.Id);

                    if (product.Quantity < p.Quantity)
                    {
                        QuantityError.Add($"庫存不足，{product.Name}只剩{product.Quantity}個!");
                    }
                }

                if (QuantityError.Count > 0)
                {
                    ViewBag.QuantityError = QuantityError;
                    return View();
                }

                try
                {
                    using var transaction = _context.Database.BeginTransaction();

                    // 儲存訂單
                    orderForm.CheckOut = "NO";
                    orderForm.CreateTime = DateTime.Now;
                    orderForm.SenderEmail = User.Identity.Name;
                    orderForm.TotalAmount = currentCart.TotalAmount;
                    _context.Add(orderForm);

                    // 先儲存才能產生訂單的Id(訂單明細會用到)
                    await _context.SaveChangesAsync();

                    // 儲存訂單明細
                    var orderDetails = new List<OrderDetail>();

                    foreach (var cartItem in currentCart)
                    {
                        orderDetails.Add(new OrderDetail()
                        {
                            OrderId = orderForm.Id,
                            Name = cartItem.Name,
                            Price = cartItem.Price,
                            Quantity = cartItem.Quantity
                        });
                    }

                    _context.OrderDetail.AddRange(orderDetails);
                    await _context.SaveChangesAsync();

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    _logger.LogError($"將第{orderForm.Id}筆訂單存入資料庫時發生錯誤...{e}");
                    return View("~/Views/Shared/DataBaseBusy.cshtml");
                }

                // 從設定檔取得 WebApi 的網域
                string MyApiDomain = ConfigManager.GetValueByKey("MyApiDomain");

                // 產生此筆交易的KEY
                string UnencryptedKey = Path.GetRandomFileName() + Path.GetRandomFileName();

                // 將 KEY 加密 & 存入Session，之後要用來驗證
                byte[] keyBytes = Encoding.UTF8.GetBytes(UnencryptedKey + string.Join("", UnencryptedKey.Reverse()));
                string EncryptedKey = Convert.ToBase64String(keyBytes);
                using (var md5 = MD5.Create())
                {
                    var result = md5.ComputeHash(Encoding.ASCII.GetBytes(EncryptedKey));
                    EncryptedKey = BitConverter.ToString(result);
                }

                HttpContext.Session.SetInt32(EncryptedKey, orderForm.Id);

                // 傳送訂單ID、未加密的KEY、購物車給 WebApi
                _logger.LogInformation($"[{orderForm.SenderEmail}]建立了第{orderForm.Id}號訂單");
                return Redirect($"{MyApiDomain}/Home/SendToOpay/?OrderKey={UnencryptedKey}&JsonString={JsonConvert.SerializeObject(currentCart)}");
            }
            else
            {
                return View();
            }
        }

        public IActionResult Edit(int? id, int returnPage = 0)
        {
            // 停用訂單編輯
            return NotFound();

            //if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            //if (returnPage != 0)
            //{
            //    HttpContext.Session.SetInt32("returnPage", returnPage);
            //}

            //if (id == null)
            //{
            //    return NotFound();
            //}

            //var orderForm = await _context.OrderForm.FindAsync(id);
            //if (orderForm == null)
            //{
            //    return NotFound();
            //}

            //return View(orderForm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, [Bind("Id,ReceiverName,ReceiverPhone,ReceiverAddress")] OrderForm orderForm)
        {
            // 停用訂單編輯
            return NotFound();

            //if (id != orderForm.Id)
            //{
            //    return NotFound();
            //}

            //if (ModelState.IsValid)
            //{
            //    try
            //    {
            //        _context.Update(orderForm);
            //        await _context.SaveChangesAsync();

            //        // 返回之前的分頁
            //        int? TryGetPage = HttpContext.Session.GetInt32("returnPage");
            //        int page = TryGetPage != null ? (int)TryGetPage : 1;
            //        return RedirectToAction("Index", new { page });
            //    }
            //    catch (DbUpdateConcurrencyException e)
            //    {
            //        _logger.LogError(e.ToString());
            //        return RedirectToAction(nameof(Index));
            //    }
            //}
            //return View(orderForm);
        }

        public async Task<IActionResult> Delete(int? id, int returnPage = 0)
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            if (returnPage != 0)
            {
                HttpContext.Session.SetInt32("returnPage", returnPage);
            }

            using var transaction = _context.Database.BeginTransaction();

            // 刪除訂單明細
            var orderDetails = _context.OrderDetail.Where(o => o.OrderId == id);
            _context.OrderDetail.RemoveRange(orderDetails);

            // 刪除訂單
            var order = await _context.OrderForm.FindAsync(id);
            _context.OrderForm.Remove(order);

            // 提交變更
            await _context.SaveChangesAsync();
            transaction.Commit();

            _logger.LogWarning($"[{User.Identity.Name}]刪除了第{order.Id}號訂單，下單者為[{order.SenderEmail}]");

            // 返回之前的分頁
            int? TryGetPage = HttpContext.Session.GetInt32("returnPage");
            int page = TryGetPage != null ? (int)TryGetPage : 1;
            return RedirectToAction("Index", new { page });
        }

        private bool OrderFormExists(int id)
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return false;

            return _context.OrderForm.Any(e => e.Id == id);
        }

        public async Task<IActionResult> CheckPayResult(bool PaySuccess=false, string OrderKey="")
        {

            if (!PaySuccess)
            {
                TempData["PayResult"] = $"付款失敗QQ...詳情請洽歐付寶的客服人員(02-2655-0115)";
                return View("PayResult");
            }

            int? GetOrderId = HttpContext.Session.GetInt32(OrderKey);
            int OrderId = 0;

            // 查看此筆交易的 Key 是否有效
            if (GetOrderId == null)
            {
                return NotFound();
            }
            else
            {
                // 若有效，則取得此筆交易的ID & 清除此筆交易的 KEY
                OrderId = (int)GetOrderId;
                HttpContext.Session.Remove(OrderKey);

                // 更新訂單狀態
                _context.OrderForm.FirstOrDefault(o => o.Id == OrderId).CheckOut = "YES";

                // 更新庫存和銷量
                Cart CurrentCart = CartManager.GetCurrentCart();

                foreach (var cartItem in CurrentCart)
                {
                    Product product = _context.Product.FirstOrDefault(m => m.Id == cartItem.Id);

                    product.Quantity -= cartItem.Quantity;
                    product.SellVolume += cartItem.Quantity;

                    // 連動更新 Product2 的庫存和銷量
                    if (product.FromProduct2)
                    {
                        Product2 product2 = _context.Product2.FirstOrDefault(m => m.Id == product.Product2Id);
                        product2.Quantity -= cartItem.Quantity;
                        product2.SellVolume += cartItem.Quantity;
                    }
                }

                await _context.SaveChangesAsync();

                // 移除購物頁面的快取，避免顯示舊的庫存
                int PageAmount = _context.Product.Count() / 9 + 1;

                for (int Page = 1; Page <= PageAmount; Page++)
                {
                    _memoryCache.Remove($"ProductPage{Page}");
                }

                // 清空購物車
                CartManager.ClearCart();

                _logger.LogInformation($"[{User.Identity.Name}]對第{OrderId}號訂單付款成功!");
                TempData["PayResult"] = $"付款成功!~請點選上方的[我的訂單]來查看付款結果。";
                return View("PayResult");
            }
        }

        public async Task<IActionResult> DeleteAll()
        {
            if (User.Identity.Name != AuthorizeManager.SuperAdmin) return NotFound();

            // 刪除訂單時，連動刪除明細
            using var transaction = _context.Database.BeginTransaction();
            _context.RemoveRange(_context.OrderDetail);
            _context.RemoveRange(_context.OrderForm);
            await _context.SaveChangesAsync();
            transaction.Commit();
            return RedirectToAction(nameof(Index));
        }
    }
}