using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
                return Content("Access denied.");
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
                return Content("Access denied.");
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
                return Content("Access denied.");
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
                return Content("Access denied.");
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
                return Content("Access denied.");
            }

            // 刪除所有產品
            _context.RemoveRange(_context.Product);
            _context.SaveChanges();

            // 匯入所有產品
            List<string> ImageUrlList = new List<string>
            {
                "https://i.imgur.com/IWPABwj.jpg",
                "https://i.imgur.com/kcJOYYc.jpg",
                "https://i.imgur.com/p0uaYGp.jpg",
                "https://i.imgur.com/VcjQCle.jpg",
                "https://i.imgur.com/9jPmqbY.jpg",
                "https://i.imgur.com/nF0DWG9.jpg",
                "https://i.imgur.com/yQWKPLf.jpg",
                "https://i.imgur.com/KNDMkAl.jpg",
                "https://i.imgur.com/PC062Dw.jpg",
                // change line for each 9 Urls
                "https://i.imgur.com/WFoheyH.jpg",
                "https://i.imgur.com/SegOy3q.jpg",
                "https://i.imgur.com/YHla8ss.jpg",
                "https://i.imgur.com/T0e33ix.jpg",
                "https://i.imgur.com/Qr0kBKg.jpg",
                "https://i.imgur.com/CM6vVEC.jpg",
                "https://i.imgur.com/0bRkLV4.jpg",
                "https://i.imgur.com/FJxXm7t.jpg",
                "https://i.imgur.com/HJ94b6p.jpg",
                // change line for each 9 Urls
                "https://i.imgur.com/UAaqt11.jpg",
                "https://i.imgur.com/G2txwGe.jpg",
                "https://i.imgur.com/btfWSC6.jpg",
                "https://i.imgur.com/14GzB19.jpg",
                "https://i.imgur.com/kKvtRWv.jpg",
                "https://i.imgur.com/CsYca4V.jpg"
            };

            List<Product> productList = new List<Product>();

            for (int i = 0; i < ImageUrlList.Count; i++)
            {
                Random random = new Random();

                productList.Add(
                    new Product
                    {
                        Name = "萌妹子壁紙" + i.ToString("D2"),
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
            // 將圖片資訊儲存在記憶體(Cache)
            _memoryCache.Set(
                "ProductCache",
                await _context.Product.OrderByDescending(p => p.PublishDate).ToPagedListAsync(page, pageSize)
            );

            // 按照產品的日期排序(新->舊)
            return View(_memoryCache.Get("ProductCache"));
        }
    }
}