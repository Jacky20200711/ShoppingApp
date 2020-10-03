using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingApp.Data;
using ShoppingApp.Models;
using X.PagedList;

namespace ShoppingApp.Controllers
{
    public class AuthorizedMemberController : Controller
    {
        //每個分頁最多顯示10筆
        private readonly int pageSize = 10;

        private readonly ApplicationDbContext _context;

        public AuthorizedMemberController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AuthorizedMember
        // 只有超級管理員可以查看特權用戶的列表
        public async Task<IActionResult> Index(int page = 1)
        {
            if (User.Identity.Name != AuthorizeManager.SuperAdmin)
            {
                return Content("404 not found");
            }

            return View(await _context.AuthorizedMember.ToPagedListAsync(page, pageSize));
        }

        // GET: AuthorizedMember/Details/5
        // 只有超級管理員可以查看特權用戶的權限
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || User.Identity.Name != AuthorizeManager.SuperAdmin)
            {
                return NotFound();
            }

            var authorizedMember = await _context.AuthorizedMember
                .FirstOrDefaultAsync(m => m.Id == id);
            if (authorizedMember == null)
            {
                return NotFound();
            }

            return View(authorizedMember);
        }

        // GET: AuthorizedMember/Create
        public IActionResult Create()
        {
            if (User.Identity.Name != AuthorizeManager.SuperAdmin)
            {
                return Content("404 not found");
            }

            return View();
        }

        // POST: AuthorizedMember/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // 只有超級管理員可以新增其他管理員
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Email,InAdminGroup,InSellerGroup")] AuthorizedMember authorizedMember)
        {
            if (User.Identity.Name != AuthorizeManager.SuperAdmin)
            {
                return Content("404 not found");
            }

            if (ModelState.IsValid)
            {
                _context.Add(authorizedMember);
                await _context.SaveChangesAsync();
                AuthorizeManager.updateHashTable(authorizedMember);
                return RedirectToAction(nameof(Index));
            }
            return View(authorizedMember);
        }

        // GET: AuthorizedMember/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || User.Identity.Name != AuthorizeManager.SuperAdmin)
            {
                return NotFound();
            }

            var authorizedMember = await _context.AuthorizedMember.FindAsync(id);
            if (authorizedMember == null || authorizedMember.Email == AuthorizeManager.SuperAdmin)
            {
                return NotFound();
            }
            return View(authorizedMember);
        }

        // POST: AuthorizedMember/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // 只有超級管理員可以編輯其他特權用戶
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Email,InAdminGroup,InSellerGroup")] AuthorizedMember authorizedMember)
        {
            if (id != authorizedMember.Id || authorizedMember.Email != AuthorizeManager.SuperAdmin)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(authorizedMember);
                    await _context.SaveChangesAsync();
                    AuthorizeManager.updateHashTable(authorizedMember);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AuthorizedMemberExists(authorizedMember.Id))
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
            return View(authorizedMember);
        }

        // GET: AuthorizedMember/Delete/5
        // 只有超級管理員可以刪除其他特權用戶
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || User.Identity.Name != AuthorizeManager.SuperAdmin)
            {
                return NotFound();
            }

            var authorizedMember = await _context.AuthorizedMember
                .FirstOrDefaultAsync(m => m.Id == id);
            if (authorizedMember == null)
            {
                return NotFound();
            }

            _context.AuthorizedMember.Remove(authorizedMember);
            await _context.SaveChangesAsync();
            AuthorizeManager.updateHashTable(authorizedMember, "delete");
            return RedirectToAction(nameof(Index));
        }

        private bool AuthorizedMemberExists(int id)
        {
            if (User.Identity.Name != AuthorizeManager.SuperAdmin)
            {
                return false;
            }

            return _context.AuthorizedMember.Any(e => e.Id == id);
        }
    }
}
