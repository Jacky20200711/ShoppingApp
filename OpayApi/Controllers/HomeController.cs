using AllPay.Payment.Integration;
using Newtonsoft.Json;
using OpayApi.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;

namespace OpayApi.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult SendToOpay(string OrderKey="", string JsonString="")
        {
            // 白名單過濾，只允許來自 Server 的 IP
            string ClientIP = HttpContext.Request.UserHostAddress;
            string ServerIP = ConfigurationManager.AppSettings["ServerIP"];
            if(ClientIP != ServerIP)
            {
                return new HttpNotFoundResult();
            }

            // 將 JsonString 轉回購物車
            Cart currentCart = JsonConvert.DeserializeObject<Cart>(JsonString);

            List<string> enErrors = new List<string>();
            string szHtml = string.Empty;
            try
            {
                using (AllInOne oPayment = new AllInOne())
                {
                    string MyApiDomain = ConfigurationManager.AppSettings["MyApiDomain"];

                    /* 服務參數 */
                    oPayment.ServiceMethod = HttpMethod.HttpPOST;
                    oPayment.ServiceURL = ConfigurationManager.AppSettings["ServiceURL"];
                    oPayment.HashKey = ConfigurationManager.AppSettings["HashKey"];
                    oPayment.HashIV = ConfigurationManager.AppSettings["HashIV"];
                    oPayment.MerchantID = ConfigurationManager.AppSettings["MerchantID"];

                    /* 基本參數 */
                    string hostname = Request.Url.Authority;
                    oPayment.Send.ReturnURL = $"{MyApiDomain}/Home/GetPayResult/?OrderKey={OrderKey}";
                    oPayment.Send.OrderResultURL = $"{MyApiDomain}/Home/GetPayResult/?OrderKey={OrderKey}";
                    oPayment.Send.MerchantTradeNo = DateTime.Now.ToString("yyyyMMddHHmmss");
                    oPayment.Send.MerchantTradeDate = DateTime.Now;
                    oPayment.Send.TotalAmount = currentCart.TotalAmount;
                    oPayment.Send.TradeDesc = "串接測試";

                    foreach (var cartItem in currentCart)
                    {
                        oPayment.Send.Items.Add(new Item()
                        {
                            Name = cartItem.Name,
                            Price = cartItem.Price,
                            Currency = "元",
                            Quantity = cartItem.Quantity
                        });
                    }

                    /* 產生歐付寶的訂單 */
                    enErrors.AddRange(oPayment.CheckOut());

                    /* 產生 Html Code */
                    enErrors.AddRange(oPayment.CheckOutString(ref szHtml));
                }
            }
            catch (Exception ex)
            {
                enErrors.Add(ex.Message);
            }
            finally
            {
                if (enErrors.Count() > 0)
                {
                    szHtml = string.Join("\\r\\n", enErrors);
                }
            }
            return Content(szHtml);
        }

        public ActionResult GetPayResult(AllInOne oPayment, string OrderKey="")
        {
            // 白名單過濾，只允許來自 Server 的 IP
            string ClientIP = HttpContext.Request.UserHostAddress;
            string ServerIP = ConfigurationManager.AppSettings["ServerIP"];
            if (ClientIP != ServerIP)
            {
                return new HttpNotFoundResult();
            }

            try
            {
                string MyAppDomain = ConfigurationManager.AppSettings["MyAppDomain"];
                List<string> enErrors = new List<string>();
                Hashtable htFeedback = null;

                oPayment.HashKey = ConfigurationManager.AppSettings["HashKey"];
                oPayment.HashIV = ConfigurationManager.AppSettings["HashIV"];
                enErrors.AddRange(oPayment.CheckOutFeedback(ref htFeedback));

                if (enErrors.Count() == 0)
                {
                    // 將 KEY 加密
                    byte[] keyBytes = Encoding.UTF8.GetBytes(OrderKey + string.Join("", OrderKey.Reverse()));
                    string EncryptedKey = Convert.ToBase64String(keyBytes);
                    using (var md5 = MD5.Create())
                    {
                        var result = md5.ComputeHash(Encoding.ASCII.GetBytes(EncryptedKey));
                        EncryptedKey = BitConverter.ToString(result);
                    }

                    return Redirect($"{MyAppDomain}/OrderForm/CheckPayResult/?OrderKey={EncryptedKey}&PaySuccess=true");
                }
                else
                {
                    return Redirect($"{MyAppDomain}/OrderForm/CheckPayResult/?PaySuccess=false");
                }
            }
            catch(Exception e)
            {
                return Content($"發生錯誤{e}，請將錯誤訊息截圖並寄給網站的管理員!");
            }
        }
    }
}
