using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShoppingApp.Data;
using ShoppingApp.Models;

namespace ShoppingApp.Controllers
{
    [Authorize]
    public class CSVController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CSVController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            return View();
        }

        public IActionResult ExportProduct()
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            CSVManager.ExportProduct(_context);

            return RedirectToAction("Index");
        }

        public IActionResult ExportUser()
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            CSVManager.ExportUser(_context);

            return RedirectToAction("Index");
        }

        public IActionResult ExportOrderForm()
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            CSVManager.ExportOrderForm(_context);

            return RedirectToAction("Index");
        }

        public IActionResult ExportComment()
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            CSVManager.ExportComment(_context);

            return RedirectToAction("Index");
        }

        public IActionResult ExportProduct2()
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            CSVManager.ExportProduct2(_context);

            return RedirectToAction("Index");
        }

        public IActionResult ExportAuthorizedMember()
        {
            if (User.Identity.Name != AuthorizeManager.SuperAdmin) return NotFound();

            CSVManager.ExportAuthorizedMember(_context);

            return RedirectToAction("Index");
        }

        public IActionResult ExportAll()
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            CSVManager.ExportAll(_context);

            return RedirectToAction("Index");
        }

        public IActionResult ImportProduct()
        {
            if (User.Identity.Name != AuthorizeManager.SuperAdmin) return NotFound();

            CSVManager.ImportProduct(_context);

            return RedirectToRoute(new { controller = "Product", action = "Index" });
        }

        public IActionResult ImportUser()
        {
            if (User.Identity.Name != AuthorizeManager.SuperAdmin) return NotFound();

            CSVManager.ImportUser(_context);

            return RedirectToRoute(new { controller = "User", action = "Index" });
        }

        public IActionResult ImportOrder()
        {
            if (User.Identity.Name != AuthorizeManager.SuperAdmin) return NotFound();

            CSVManager.ImportOrder(_context);

            return RedirectToRoute(new { controller = "OrderForm", action = "Index" });
        }

        public IActionResult ImportComment()
        {
            if (User.Identity.Name != AuthorizeManager.SuperAdmin) return NotFound();

            CSVManager.ImportComment(_context);

            return RedirectToRoute(new { controller = "Comment", action = "Index" });
        }

        public IActionResult ImportProduct2()
        {
            if (User.Identity.Name != AuthorizeManager.SuperAdmin) return NotFound();

            CSVManager.ImportProduct2(_context);

            return RedirectToRoute(new { controller = "Product2", action = "Index" });
        }

        public IActionResult ImportAuthorizedMember()
        {
            if (User.Identity.Name != AuthorizeManager.SuperAdmin) return NotFound();

            CSVManager.ImportAuthorizedMember(_context);

            return RedirectToRoute(new { controller = "AuthorizedMember", action = "Index" });
        }
    }
}
