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

        //清空購物車，並回傳購物車頁面
        public IActionResult ClearCart()
        {
            CartOperator.ClearCart();

            return PartialView("_CartPartial");
        }
    }
}