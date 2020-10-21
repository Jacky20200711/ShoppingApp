using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingApp.Data;
using ShoppingApp.Models;
using X.PagedList;

namespace ShoppingApp.Controllers
{
    [Authorize]
    public class AuthorizedMemberController : Controller
    {
        // 每個分頁最多顯示10筆
        private readonly int pageSize = 10;

        // 注入會用到的工具
        private readonly ApplicationDbContext _context;

        public AuthorizedMemberController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            if (User.Identity.Name != AuthorizeManager.SuperAdmin) return NotFound();

            return View(await _context.AuthorizedMember.ToPagedListAsync(page, pageSize));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (User.Identity.Name != AuthorizeManager.SuperAdmin) return NotFound();

            if (id == null)
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

        public IActionResult Create()
        {
            if (User.Identity.Name != AuthorizeManager.SuperAdmin) return NotFound();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Email,InAdminGroup,InSellerGroup")] AuthorizedMember authorizedMember)
        {
            if (User.Identity.Name != AuthorizeManager.SuperAdmin) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Add(authorizedMember);
                await _context.SaveChangesAsync();
                AuthorizeManager.UpdateAuthority("UpdateHashTableByAuthorizedMember", null, null, null, authorizedMember);
                return RedirectToAction(nameof(Index));
            }
            return View(authorizedMember);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (User.Identity.Name != AuthorizeManager.SuperAdmin) return NotFound();

            if (id == null)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Email,InAdminGroup,InSellerGroup")] AuthorizedMember authorizedMember)
        {
            if (User.Identity.Name != AuthorizeManager.SuperAdmin) return NotFound();

            // 令超級管理員無法被編輯
            if (authorizedMember.Email == AuthorizeManager.SuperAdmin) return NotFound();

            if (id != authorizedMember.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(authorizedMember);
                    await _context.SaveChangesAsync();
                    AuthorizeManager.UpdateAuthority("UpdateHashTableByAuthorizedMember", _context, null, null, authorizedMember);
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

        public async Task<IActionResult> Delete(int? id)
        {
            if (User.Identity.Name != AuthorizeManager.SuperAdmin) return NotFound();

            if (id == null)
            {
                return NotFound();
            }

            var authorizedMember = await _context.AuthorizedMember
                .FirstOrDefaultAsync(m => m.Id == id);
            if (authorizedMember == null)
            {
                return NotFound();
            }

            // 令超級管理員無法被刪除
            if (authorizedMember.Email == AuthorizeManager.SuperAdmin) return NotFound();

            AuthorizeManager.UpdateAuthority("DeleteAll", _context, authorizedMember.Email, null, null);
            return RedirectToAction(nameof(Index));
        }

        private bool AuthorizedMemberExists(int id)
        {
            if (User.Identity.Name != AuthorizeManager.SuperAdmin) return false;

            return _context.AuthorizedMember.Any(e => e.Id == id);
        }
    }
}
