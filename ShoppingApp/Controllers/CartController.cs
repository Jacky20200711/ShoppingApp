using Microsoft.AspNetCore.Mvc;
using ShoppingApp.Data;
using ShoppingApp.Models;

namespace ShoppingApp.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult AddToCart(int id)
        {
            CartManager.AddProduct(id, _context);

            return PartialView("_CartPartial");
        }

        public IActionResult RemoveFromCart(int id)
        {
            CartManager.RemoveProduct(id);

            return PartialView("_CartPartial");
        }

        // 這個 Action 用來讓 User 可以在填寫訂單的頁面，移除不想要的產品並刷新頁面
        public IActionResult RefreshAfterRemove(int id)
        {
            CartManager.RemoveProduct(id);

            return RedirectToRoute(new { controller = "OrderForm", action = "Create" });
        }

        public IActionResult ClearCart()
        {
            CartManager.ClearCart();

            return PartialView("_CartPartial");
        }
    }
}