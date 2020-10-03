using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingApp.Data;
using ShoppingApp.Models;
using X.PagedList;

namespace ShoppingApp.Controllers
{
    public class CommentController : Controller
    {
        //每個分頁最多顯示10筆
        private readonly int pageSize = 10;

        // 使用 DI 注入會用到的工具
        private readonly ApplicationDbContext _context;

        public CommentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Comment
        public async Task<IActionResult> Index(int page = 1)
        {
            if (!RightChecker.inAdminGroup(User.Identity.Name))
            {
                return Content("404 not found");
            }

            // 按照留言的建立日期排序(新->舊)
            return View(await _context.Comment.OrderByDescending(c => c.CreateTime).ToPagedListAsync(page, pageSize));
        }

        // GET: Comment/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!RightChecker.inAdminGroup(User.Identity.Name))
            {
                return Content("404 not found");
            }

            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.Comment
                .FirstOrDefaultAsync(m => m.Id == id);
            if (comment == null)
            {
                return NotFound();
            }

            return View(comment);
        }

        // GET: Comment/Create
        public IActionResult Create()
        {
            if (!RightChecker.inAdminGroup(User.Identity.Name))
            {
                return Content("404 not found");
            }

            return View();
        }

        // POST: Comment/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Content,UserName,CreateTime,ProductId")] Comment comment)
        {
            if (!RightChecker.inAdminGroup(User.Identity.Name))
            {
                return Content("404 not found");
            }

            if (ModelState.IsValid)
            {
                _context.Add(comment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(comment);
        }

        // GET: Comment/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!RightChecker.inAdminGroup(User.Identity.Name))
            {
                return Content("404 not found");
            }

            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.Comment.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }
            return View(comment);
        }

        // POST: Comment/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Content,UserName,CreateTime,ProductId")] Comment comment)
        {
            if (!RightChecker.inAdminGroup(User.Identity.Name))
            {
                return Content("404 not found");
            }

            if (id != comment.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(comment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CommentExists(comment.Id))
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
            return View(comment);
        }

        // GET: Comment/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!RightChecker.inAdminGroup(User.Identity.Name))
            {
                return Content("404 not found");
            }

            var comment = await _context.Comment.FindAsync(id);
            _context.Comment.Remove(comment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CommentExists(int id)
        {
            return _context.Comment.Any(e => e.Id == id);
        }
    }
}