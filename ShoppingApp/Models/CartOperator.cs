using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using ShoppingApp.Data;
using System.Linq;

namespace ShoppingApp.Models
{
    public static class CartOperator
    {
        // 注入 HttpContextAccessor ，讓這個類別可以利用 HttpContext 來存取 Session
        private static IHttpContextAccessor _contextAccessor;

        public static HttpContext Current => _contextAccessor.HttpContext;

        internal static void Configure(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public static Cart GetCurrentCart()
        {
            if (_contextAccessor.HttpContext.Session.GetString("Cart") == null)
            {
                _contextAccessor.HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(new Cart()));
            }

            return JsonConvert.DeserializeObject<Cart>(_contextAccessor.HttpContext.Session.GetString("Cart"));
        }

        public static void AddProduct(int id, ApplicationDbContext db)
        {
            // 取得 Session 中的購物車
            var GuestCart = GetCurrentCart();

            // 判斷購物車內是否有此 Id 的商品
            var findItem = GuestCart.cartItems.Where(s => s.Id == id).FirstOrDefault();

            if (GuestCart.Contains(findItem))
            {
                findItem.Quantity += 1;
            }
            else
            {
                // 若此項ID不存在於購物車內，則從資料庫撈取產品並加入購物車
                Product product = db.Product.Where(s => s.Id == id).FirstOrDefault();

                GuestCart.Add(new CartItem()
                {
                    Id = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    Quantity = 1,
                    DefaultImageURL = product.DefaultImageURL
                });
            }

            // 更新 Session 中的購物車
            _contextAccessor.HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(GuestCart));
        }


        public static void RemoveProduct(int id)
        {
            var GuestCart = GetCurrentCart();

            var findItem = GuestCart.cartItems.Where(s => s.Id == id).FirstOrDefault();
            GuestCart.Remove(findItem);

            // 更新 Session 中的購物車
            _contextAccessor.HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(GuestCart));
        }

        public static void ClearCart()
        {
            var GuestCart = GetCurrentCart();

            GuestCart.Clear();

            // 更新 Session 中的購物車
            _contextAccessor.HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(GuestCart));
        }
    }
}