﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using ShoppingApp.Data;
using ShoppingApp.Models;
using X.PagedList;

namespace ShoppingApp.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        private static IMemoryCache _memoryCache;

        //每個分頁最多顯示9筆
        private readonly int pageSize = 9;

        public ProductController(ApplicationDbContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _memoryCache = memoryCache;
        }

        // GET: Product
        public async Task<IActionResult> Index(int page = 1)
        {
            if (User.Identity.Name != Admin.name)
            {
                return Content("404 not found");
            }

            // 按照產品的日期排序(新->舊)
            return View(await _context.Product.OrderByDescending(p => p.PublishDate).ToPagedListAsync(page, 10));
        }

        // GET: Product/Details/5
        public async Task<IActionResult> Details(int? id, int page = 1)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Product
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            // 取得此產品的所有留言
            var TheComments = _context.Comment.Where(c => c.ProductId == id).OrderByDescending(c => c.CreateTime).ToList();

            // 封裝此產品和此產品的留言
            List<ProductAndComment> productAndComments = new List<ProductAndComment>();

            if (TheComments.Count > 0)
            {
                foreach (var comment in TheComments)
                {
                    productAndComments.Add(new ProductAndComment
                    {
                        TheProduct = product,
                        TheComment = comment
                    });
                };
            }
            else
            {
                // 若此產品沒有留言，則添加產品資訊即可
                productAndComments.Add(new ProductAndComment
                {
                    TheProduct = product,
                    TheComment = null
                });
            }

            // 傳送封裝的類別，每頁顯示10筆留言
            return View(await productAndComments.ToPagedListAsync(page, 10));
        }

        // GET: Product/Create
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Product/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Price,PublishDate,Quantity,DefaultImageURL")] Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Product/Edit/5
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

            var product = await _context.Product.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Product/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,PublishDate,Quantity,DefaultImageURL")] Product product)
        {
            if (User.Identity.Name != Admin.name)
            {
                return Content("404 not found");
            }

            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
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
            return View(product);
        }

        // GET: Product/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (User.Identity.Name != Admin.name)
            {
                return Content("404 not found");
            }

            var product = await _context.Product.FindAsync(id);
            _context.Product.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Product.Any(e => e.Id == id);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddComment(int id, string comment)
        {
            if (string.IsNullOrEmpty(comment) || comment.Length < 2 || comment.Length > 100)
            {
                return Content("輸入長度有誤!");
            }
            else
            {
                await _context.Comment.AddAsync(new Comment
                {
                    UserName = User.Identity.Name,
                    ProductId = id,
                    CreateTime = DateTime.Now,
                    Content = comment
                });
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", new { id });
            }
        }

        // 重置產品
        public ActionResult ResetProducts()
        {
            if (User.Identity.Name != Admin.name)
            {
                return Content("404 not found");
            }

            // 刪除所有產品
            _context.RemoveRange(_context.Product);
            _context.SaveChanges();

            // 從設定檔取得壁紙的網址
            List<string> ImageUrlList = new List<string>();

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            var config = builder.Build().AsEnumerable();

            foreach (KeyValuePair<string, string> pair in config)
            {
                if(pair.Key.StartsWith("WallPaper") && pair.Value != null)
                {
                    ImageUrlList.Add(pair.Value);
                }
            }

            // 重新創建所有的產品
            List<Product> productList = new List<Product>();

            for (int i = 0; i < ImageUrlList.Count; i++)
            {
                Random random = new Random();

                productList.Add(
                    new Product
                    {
                        Name = "萌妹子壁紙" + (i+1).ToString("D2"),
                        Description = "可愛的萌妹子壁紙",
                        Price = random.Next(100, 200),
                        PublishDate = DateTime.Now,
                        Quantity = 200,
                        DefaultImageURL = ImageUrlList[i]
                    }
                );
            }

            _context.AddRange(productList);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // 負責展示產品的Action
        public async Task<IActionResult> ShowProducts(int page = 1)
        {
            // 將圖片資訊儲存在記憶體(Cache)，每個分頁對應不同的Key
            _memoryCache.Set(
                $"ProductPage{page}",
                await _context.Product.OrderByDescending(p => p.PublishDate).ToPagedListAsync(page, pageSize)
            );

            // 按照產品的日期排序(新->舊)
            return View(_memoryCache.Get($"ProductPage{page}"));
        }
    }
}