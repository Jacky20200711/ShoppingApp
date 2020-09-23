using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using ShoppingApp.Data;
using System.Linq;

namespace ShoppingApp.Models
{
    public static class CartOperator
    {
        /// <summary>
        /// 注入 HttpContextAccessor ，讓這個類別可以利用 HttpContext 來存取 Session
        private static IHttpContextAccessor _contextAccessor;

        public static HttpContext Current => _contextAccessor.HttpContext;

        internal static void Configure(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }
        /// </summary>

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

            var findItem = GuestCart.cartItems.Where(s => s.Id == id).FirstOrDefault();

            //判斷購物車內是否有此ID的商品
            if (GuestCart.Contains(findItem))
            {
                findItem.Quantity += 1;
            }
            else
            {
                //此項ID不存在於購物車內，從資料庫撈取產品並加入購物車
                Product product = db.Product.Where(s => s.Id == id).FirstOrDefault();

                GuestCart.Add(new CartItem()
                {
                    Id = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    Quantity = 1
                });
            }

            // 更新 Session 中的購物車
            _contextAccessor.HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(GuestCart));
        }


        //移除相同ID的Product
        public static void RemoveProduct(int id)
        {
            // 取得 Session 中的購物車
            var GuestCart = GetCurrentCart();

            var findItem = GuestCart.cartItems.Where(s => s.Id == id).FirstOrDefault();

            GuestCart.Remove(findItem);

            // 更新 Session 中的購物車
            _contextAccessor.HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(GuestCart));
        }

        //清空購物車
        public static void ClearCart()
        {
            // 取得 Session 中的購物車
            var GuestCart = GetCurrentCart();

            GuestCart.Clear();

            // 更新 Session 中的購物車
            _contextAccessor.HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(GuestCart));
        }
    }
}