using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using ShoppingApp.Data;
using ShoppingApp.Models;
using X.PagedList;

namespace ShoppingApp.Controllers
{
    public class OrderFormController : Controller
    {
        //每個分頁最多顯示10筆
        private readonly int pageSize = 10;

        private readonly ApplicationDbContext _context;

        public OrderFormController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: OrderForm
        [Authorize]
        public async Task<IActionResult> Index(int page = 1)
        {
            if (User.Identity.Name != Admin.name)
            {
                // 返回該USER的訂單，並按照日期排序(新->舊)
                return View(await _context.OrderForm.Where(o => o.SenderEmail == User.Identity.Name).OrderByDescending(o => o.CreateTime).ToPagedListAsync(page, pageSize));
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
        public async Task<IActionResult> Create([Bind("Id,ReceiverName,ReceiverPhone,ReceiverAddress,SenderEmail,CreateTime,TotalAmount,CheckOut")] OrderForm orderForm)
        {
            if (ModelState.IsValid)
            {
                var currentCart = CartOperator.GetCurrentCart();

                using (var transaction = _context.Database.BeginTransaction())
                {
                    // 儲存訂單
                    orderForm.CheckOut = "NO";
                    orderForm.CreateTime = DateTime.Now;
                    orderForm.SenderEmail = User.Identity.Name;
                    orderForm.TotalAmount = currentCart.TotalAmount;
                    _context.Add(orderForm);
                    await _context.SaveChangesAsync();

                    // 儲存訂單明細
                    var orderDetails = new List<OrderDetail>();
                    var orderId = orderForm.Id;

                    foreach (var cartItem in currentCart)
                    {
                        orderDetails.Add(new OrderDetail()
                        {
                            OrderId = orderId,
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

                // 存取 WebApi 的網域
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json");

                var config = builder.Build();

                string ApiDomain = config["AppSetting:ApiDomain"];

                return Redirect($"{ApiDomain}/Home/SendToOpay/?JsonString={JsonConvert.SerializeObject(currentCart)}");
            }
            else
            {
                return Content("訂單建立失敗，請查看您的填寫內容、或購物車是否為空。");
            }
        }

        // GET: OrderForm/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (User.Identity.Name != Admin.name)
            {
                return Content("Access denied.");
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,ReceiverName,ReceiverPhone,ReceiverAddress,SenderEmail,CreateTime,TotalAmount,CheckOut")] OrderForm orderForm)
        {
            if (User.Identity.Name != Admin.name)
            {
                return Content("Access denied.");
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
                return Content("Access denied.");
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
    }
}