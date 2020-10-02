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

        public IActionResult Index()
        {
            return View();
        }

        //加入特定ID的商品到購物車，並回傳購物車頁面
        public IActionResult AddToCart(int id)
        {
            CartOperator.AddProduct(id, _context);

            return PartialView("_CartPartial");
        }

        //移除特定ID的產品
        public IActionResult RemoveFromCart(int id)
        {
            CartOperator.RemoveProduct(id);

            return PartialView("_CartPartial");
        }

        //這個 Action 用來讓 User 可以在填寫訂單的頁面，移除不想要的產品並刷新頁面
        //先移除產品，再刷新頁面
        public IActionResult RefreshAfterRemove(int id)
        {
            CartOperator.RemoveProduct(id);

            return RedirectToRoute(new { controller = "OrderForm", action = "Create" });
        }

        //清空購物車，並回傳購物車頁面
        public IActionResult ClearCart()
        {
            CartOperator.ClearCart();

            return PartialView("_CartPartial");
        }
    }
}