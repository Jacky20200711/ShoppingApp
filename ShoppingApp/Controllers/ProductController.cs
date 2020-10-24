using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShoppingApp.Data;
using ShoppingApp.Models;
using X.PagedList;

namespace ShoppingApp.Controllers
{
    public class ProductController : Controller
    {
        // 每個購物分頁最多顯示9筆，每個管理分頁最多顯示10筆
        private readonly int pageSize = 9;  
        private readonly int pageSize2 = 10;

        // 注入會用到的工具
        private readonly ApplicationDbContext _context;
        private static IMemoryCache _memoryCache;
        private readonly ILogger _logger;

        public ProductController(
            ApplicationDbContext context, 
            IMemoryCache memoryCache, 
            ILogger<ProductController> logger)
        {
            _context = context;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            // 以當前 Session 的排序類型做排序 
            return (HttpContext.Session.GetString("SortType")) switch
            {
                "Date" => View(await _context.Product.OrderByDescending(p => p.PublishDate).ToPagedListAsync(page, pageSize2)),
                "Sell" => View(await _context.Product.OrderByDescending(p => p.SellVolume).ToPagedListAsync(page, pageSize2)),
                _ => View(await _context.Product.OrderByDescending(p => p.PublishDate).ToPagedListAsync(page, pageSize2)),
            };
        }

        public async Task<IActionResult> SortByDate(int page = 1)
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            HttpContext.Session.SetString("SortType", "Date");

            return View("Index", await _context.Product.OrderByDescending(p => p.PublishDate).ToPagedListAsync(page, pageSize2));
        }

        public async Task<IActionResult> SortBySell(int page = 1)
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            HttpContext.Session.SetString("SortType", "Sell");

            return View("Index", await _context.Product.OrderByDescending(p => p.SellVolume).ToPagedListAsync(page, pageSize2));
        }

        public IActionResult GetProfit()
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            int profit = 0;

            // 排除來自賣方的產品
            List<Product> ProductList = _context.Product.Where(m => m.FromProduct2 == false).ToList();

            foreach(Product product in ProductList)
            {
                profit += product.Price * product.SellVolume;
            }

            // 使用 Session 持久化儲存，讓這個值可以持續顯示在頁面
            HttpContext.Session.SetString("TotalProfit", $"總共獲利 : {profit}");

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Details(int? id, int page = 1, int returnPage = 0)
        {
            // 紀錄之前所在的購物分頁，為了讓 User 可以回到之前的購物分頁
            // 只有當 User 點選了查看留言，才會令 returnPage 不會為零
            if (returnPage != 0)
            {
                HttpContext.Session.SetInt32("returnPage", returnPage);
            }

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

        public IActionResult Create(int returnPage = 0)
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            if (returnPage != 0)
            {
                HttpContext.Session.SetInt32("returnPage", returnPage);
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Price,Quantity,DefaultImageURL")] Product product)
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            if (ModelState.IsValid)
            {
                product.PublishDate = DateTime.Now;
                product.FromProduct2 = false;
                product.SellVolume = 0;
                _context.Add(product);
                await _context.SaveChangesAsync();

                // 返回之前的分頁
                int? TryGetPage = HttpContext.Session.GetInt32("returnPage");
                int page = TryGetPage != null ? (int)TryGetPage : 1;
                return RedirectToAction("Index", new { page });
            }
            return View(product);
        }

        public async Task<IActionResult> Edit(int? id, int returnPage = 0)
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            // 紀錄之前所在的分頁
            if (returnPage != 0)
            {
                HttpContext.Session.SetInt32("returnPage", returnPage);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,Quantity,DefaultImageURL")] Product product)
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    Product prod = _context.Product.FirstOrDefault(m => m.Id == id);
                    prod.Name = product.Name;
                    prod.Description = product.Description;
                    prod.Price = product.Price;
                    prod.Quantity = product.Quantity;
                    prod.DefaultImageURL = product.DefaultImageURL;
                    await _context.SaveChangesAsync();

                    // 返回之前的分頁
                    int? TryGetPage = HttpContext.Session.GetInt32("returnPage");
                    int page = TryGetPage != null ? (int)TryGetPage : 1;
                    return RedirectToAction("Index", new { page });
                }
                catch (DbUpdateConcurrencyException e)
                {
                    _logger.LogError(e.ToString());
                    return RedirectToAction(nameof(Index));
                }
            }
            return View(product);
        }

        public async Task<IActionResult> Delete(int? id, int returnPage = 0)
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            var product = await _context.Product.FindAsync(id);
            _context.Product.Remove(product);
            await _context.SaveChangesAsync();
            _logger.LogWarning($"[{User.Identity.Name}]刪除了產品[{product.Name}]");

            // 返回之前的分頁
            int page = returnPage != 0 ? returnPage : 1;
            return RedirectToAction("Index", new { page });
        }

        public async Task<IActionResult> DeleteAll()
        {
            if (User.Identity.Name != AuthorizeManager.SuperAdmin) return NotFound();

            _context.RemoveRange(_context.Product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return false;

            return _context.Product.Any(e => e.Id == id);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddComment(int id, string comment)
        {
            string ClientIP = HttpContext.Connection.RemoteIpAddress.ToString();

            // 檢查這個IP的留言次數
            if (CommentManager.GetCommentCountByIP(ClientIP) > 4)
            {
                TempData["ProductDetail"] = "您的留言次數已達上限，請聯絡網站的管理員!";

                return RedirectToAction("Details", new { id });
            }
            else
            {
                CommentManager.IncrementCount(ClientIP);
            }

            // 檢查留言長度
            if (string.IsNullOrEmpty(comment) || comment.Length < 2 || comment.Length > 100)
            {
                TempData["ProductDetail"] = "請檢查您的留言長度!";
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
            }
            return RedirectToAction("Details", new { id });
        }

        public ActionResult ResetProducts()
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            // 刪除所有產品 & Reset產品資料表的Id
            _context.RemoveRange(_context.Product);
            _context.SaveChanges();
            _context.Database.ExecuteSqlRaw("DBCC CHECKIDENT('Product', RESEED, 0)");

            // 從設定檔取得產品的網址
            List<string> ImageUrlList = new List<string>();

            var config = ConfigManager.GetAllPair();

            foreach (KeyValuePair<string, string> pair in config)
            {
                if(pair.Key.StartsWith("WallPaper") && pair.Value != null)
                {
                    ImageUrlList.Add(pair.Value);
                }
            }

            // 重新創建所有產品
            List<Product> productList = new List<Product>();

            for (int i = 0; i < ImageUrlList.Count; i++)
            {
                Random random = new Random();

                productList.Add(
                    new Product
                    {
                        Name = "萌妹壁紙" + (i + 1).ToString("D2"),
                        Description = "可愛的萌妹壁紙",
                        Price = random.Next(100, 200),
                        PublishDate = DateTime.Now.AddSeconds(i),
                        Quantity = random.Next(30, 50),
                        DefaultImageURL = ImageUrlList[i],
                        FromProduct2 = false,
                        SellVolume = random.Next(30, 50) - 25
                    }
                );
            }

            _context.AddRange(productList);
            _context.SaveChanges();
            ClearCache();
            return RedirectToAction("ShowProducts");
        }

        public async Task<IActionResult> ShowProducts(int page = 1)
        {
            // 從 Cache 取出這一頁的產品資訊
            if (_memoryCache.Get($"ProductPage{page}") != null)
            {
                return View(_memoryCache.Get($"ProductPage{page}"));
            }
            // 將這一頁的產品資訊存入 Cache
            else
            {
                _memoryCache.Set(
                    $"ProductPage{page}",
                    await _context.Product.OrderByDescending(p => p.PublishDate).ToPagedListAsync(page, pageSize)
                );

                return View(_memoryCache.Get($"ProductPage{page}"));
            }
        }

        public IActionResult ClearCache()
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            // 清除所有購物分頁的 Cache
            int PageAmount = _context.Product.Count() / 9 + 1;

            for (int Page = 1; Page <= PageAmount; Page++)
            {
                _memoryCache.Remove($"ProductPage{Page}");
            }

            return RedirectToAction("ShowProducts");
        }
    }
}