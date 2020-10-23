using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using ShoppingApp.Data;
using System.Linq;

namespace ShoppingApp.Models
{
    public static class CartOperator
    {
        // 注入 HttpContextAccessor ，讓這個類別可以存取 Session
        private static IHttpContextAccessor _contextAccessor;

        internal static void Configure(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public static Cart GetCurrentCart()
        {
            // 若 Session 中沒有購物車則創建一個
            if (_contextAccessor.HttpContext.Session.GetString("Cart") == null)
            {
                _contextAccessor.HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(new Cart()));
            }

            // 返回 Session 中的購物車
            return JsonConvert.DeserializeObject<Cart>(_contextAccessor.HttpContext.Session.GetString("Cart"));
        }

        public static void AddProduct(int id, ApplicationDbContext db)
        {
            var GuestCart = GetCurrentCart();
            var findItem = GuestCart.cartItems.FirstOrDefault(s => s.Id == id);

            // 判斷購物車內是否有此 Id 的商品
            if (GuestCart.Contains(findItem))
            {
                findItem.Quantity += 1;
            }
            else
            {
                Product product = db.Product.FirstOrDefault(s => s.Id == id);

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
            var findItem = GuestCart.cartItems.FirstOrDefault(s => s.Id == id);
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