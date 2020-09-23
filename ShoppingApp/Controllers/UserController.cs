using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShoppingApp.Data;
using ShoppingApp.Models;
using X.PagedList;

namespace ShoppingApp.Controllers
{
    public class UserController : Controller
    {
        //每個分頁最多顯示10筆
        private readonly int pageSize = 10;

        private readonly ApplicationDbContext _usertext;

        public UserController(ApplicationDbContext usertext)
        {
            _usertext = usertext;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            if (User.Identity.Name != Admin.name)
            {
                return Content("Access denied.");
            }

            return View(await _usertext.Users.ToPagedListAsync(page, pageSize));
        }

        public ActionResult Delete(string id)
        {
            if (User.Identity.Name != Admin.name)
            {
                return Content("Access denied.");
            }

            var user = _usertext.Users.Where(u => u.Id == id).FirstOrDefault();

            // 令管理員不能刪除自己
            if (user.Email == Admin.name)
            {
                return Content("Access denied.");
            }

            _usertext.Users.Remove(user);
            _usertext.SaveChanges();

            return RedirectToAction("Index");
        }

        public ActionResult Edit(string id)
        {
            if (User.Identity.Name != Admin.name)
            {
                return Content("Access denied.");
            }

            var user = _usertext.Users.Where(u => u.Id == id).FirstOrDefault();

            return View(user);
        }

        [HttpPost]
        public ActionResult Edit(IdentityUser identityUser)
        {
            if (User.Identity.Name != Admin.name)
            {
                return Content("Access denied.");
            }

            var user = _usertext.Users.Where(u => u.Id == identityUser.Id).FirstOrDefault();

            if (user.Email == Admin.name)
            {
                return Content("Access denied.");
            }

            PasswordHasher<IdentityUser> PwHasher = new PasswordHasher<IdentityUser>();

            user.Email = identityUser.Email;
            user.PasswordHash = PwHasher.HashPassword(user, user.PasswordHash);

            _usertext.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}