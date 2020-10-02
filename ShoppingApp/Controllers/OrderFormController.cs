using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CartsCore.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ShoppingApp.Data;
using ShoppingApp.Models;
using SQLitePCL;
using X.PagedList;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Security.Cryptography;

namespace ShoppingApp.Controllers
{
    public class OrderFormController : Controller
    {
        // 每個分頁最多顯示10筆
        private readonly int pageSize = 10;

        // 使用 DI 注入會用到的工具
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly UserManager<IdentityUser> _userManager;

        public OrderFormController(
                ApplicationDbContext context, 
                ILogger<OrderFormController> logger,
                UserManager<IdentityUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        // GET: OrderForm
        [Authorize]
        public async Task<IActionResult> Index(int page = 1)
        {
            if (User.Identity.Name != Admin.name)
            {
                // 返回該 UserId 所下的訂單，並按照日期排序(新->舊)
                return View(await _context.OrderForm.Where(o => o.SenderId == User.FindFirstValue(ClaimTypes.NameIdentifier)).OrderByDescending(o => o.CreateTime).ToPagedListAsync(page, pageSize));
            }
            else
            {
                // 如果是管理員，則返回所有人的訂單
                return View(await _context.OrderForm.OrderByDescending(o => o.CreateTime).ToPagedListAsync(page, pageSize));
            }
        }

        // GET: OrderForm/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderForm = await _context.OrderForm
                .FirstOrDefaultAsync(m => m.Id == id);
            if (orderForm == null)
            {
                return NotFound();
            }

            return View(_context.OrderDetail.Where(o => o.OrderId == id).ToList());
        }

        // GET: OrderForm/Create
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // POST: OrderForm/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SenderId,ReceiverName,ReceiverPhone,ReceiverAddress,SenderEmail,CreateTime,TotalAmount,CheckOut")] OrderForm orderForm)
        {
            var currentCart = CartOperator.GetCurrentCart();

            if (ModelState.IsValid && currentCart.TotalAmount > 0)
            {
                try
                {
                    using var transaction = _context.Database.BeginTransaction();

                    // 儲存訂單
                    orderForm.SenderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    orderForm.CheckOut = "NO";
                    orderForm.CreateTime = DateTime.Now;
                    orderForm.SenderEmail = User.Identity.Name;
                    orderForm.TotalAmount = currentCart.TotalAmount;
                    _context.Add(orderForm);
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

                    // 提交Transaction
                    transaction.Commit();
                }
                catch (Exception e)
                {
                    _logger.LogError($"將第{orderForm.Id}筆訂單存入資料庫時發生錯誤...{e}");
                    return View("~/Views/Shared/DataBaseBusy.cshtml");
                }

                // 存取 WebApi 的網域
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json");

                var config = builder.Build();

                string MyApiDomain = config["AppSetting:MyApiDomain"];

                // 產生此筆交易的KEY
                string UnencryptedKey = Path.GetRandomFileName() + Path.GetRandomFileName();

                // 將 KEY 加密
                byte[] keyBytes = Encoding.UTF8.GetBytes(UnencryptedKey + string.Join("", UnencryptedKey.Reverse()));
                string EncryptedKey = Convert.ToBase64String(keyBytes);
                using (var md5 = MD5.Create())
                {
                    var result = md5.ComputeHash(Encoding.ASCII.GetBytes(EncryptedKey));
                    EncryptedKey = BitConverter.ToString(result);
                }

                HttpContext.Session.SetInt32(EncryptedKey, orderForm.Id);

                // 傳送訂單ID、此筆交易的KEY、購物車給 WebApi
                _logger.LogInformation($"[{orderForm.SenderEmail}]建立了第{orderForm.Id}號訂單");
                return Redirect($"{MyApiDomain}/Home/SendToOpay/?OrderKey={UnencryptedKey}&JsonString={JsonConvert.SerializeObject(currentCart)}");
            }
            else
            {
                // 遇到不合法的訂單，直接導回當前頁面
                return View();
            }
        }

        // GET: OrderForm/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (User.Identity.Name != Admin.name)
            {
                return Content("404 not found");
            }

            if (id == null)
            {
                return NotFound();
            }

            var orderForm = await _context.OrderForm.FindAsync(id);
            if (orderForm == null)
            {
                return NotFound();
            }
            return View(orderForm);
        }

        // POST: OrderForm/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,SenderId,ReceiverName,ReceiverPhone,ReceiverAddress,SenderEmail,CreateTime,TotalAmount,CheckOut")] OrderForm orderForm)
        {
            if (User.Identity.Name != Admin.name)
            {
                return Content("404 not found");
            }

            if (id != orderForm.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(orderForm);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderFormExists(orderForm.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(orderForm);
        }

        // GET: OrderForm/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (User.Identity.Name != Admin.name)
            {
                return Content("404 not found");
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                // 刪除訂單明細並更新資料庫
                var orderDetails = _context.OrderDetail.Where(o => o.OrderId == id);
                _context.OrderDetail.RemoveRange(orderDetails);
                await _context.SaveChangesAsync();

                // 刪除訂單並更新資料庫
                var order = await _context.OrderForm.FindAsync(id);
                _context.OrderForm.Remove(order);
                await _context.SaveChangesAsync();

                // 提交變更
                transaction.Commit();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool OrderFormExists(int id)
        {
            return _context.OrderForm.Any(e => e.Id == id);
        }

        [Authorize]

        public IActionResult CheckPayResult(bool PaySuccess=false, string Exception="", string OrderKey="")
        {
            int? GetOrderId = HttpContext.Session.GetInt32(OrderKey);
            int OrderId = 0;

            // 查看此筆交易的 Key 是否有效
            if (GetOrderId == null)
            {
                _logger.LogWarning($"[{User.Identity.Name}]提供的 KEY={OrderKey} 為無效");
                TempData["PayResult"] = $"歡迎光臨 ^___^ 您必須在付款後才能查看付款結果歐~";
                return View("PayResult");
            }
            else
            {
                // 若有效，則取得此筆交易的ID & 清除此筆交易的 KEY
                OrderId = (int)GetOrderId;
                HttpContext.Session.Remove(OrderKey);
            }

            if (PaySuccess)
            {
                _context.OrderForm.FirstOrDefault(o => o.Id == OrderId).CheckOut = "YES";
                _context.SaveChanges();
                _logger.LogInformation($"[{User.Identity.Name}]對第{OrderId}號訂單付款成功!");
                TempData["PayResult"] = $"付款成功!~請點選[我的訂單]來查看付款結果。";
                CartOperator.ClearCart();
                return View("PayResult");
            }
            else
            {
                _logger.LogError($"[{User.Identity.Name}]對第{OrderId}號訂單付款失敗..." + Exception);
                TempData["PayResult"] = $"付款失敗QQ...詳情請洽歐付寶的客服人員(02-2655-0115)";
                return View("PayResult");
            }
        }
    }
}