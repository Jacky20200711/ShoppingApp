using AllPay.Payment.Integration;
using Newtonsoft.Json;
using OpayApi.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OpayApi.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult SendToOpay(int OrderId, string JsonString)
        {
            // 用 Session 暫存 OrderId
            Session["OrderId"] = OrderId;

            // 將 JsonString 轉回購物車
            Cart currentCart = JsonConvert.DeserializeObject<Cart>(JsonString);

            List<string> enErrors = new List<string>();
            string szHtml = string.Empty;
            try
            {
                using (AllInOne oPayment = new AllInOne())
                {
                    string ApiDomain = ConfigurationManager.AppSettings["ApiDomain"];

                    /* 服務參數 */
                    oPayment.ServiceMethod = HttpMethod.HttpPOST;
                    oPayment.ServiceURL = ConfigurationManager.AppSettings["ServiceURL"];
                    oPayment.HashKey = ConfigurationManager.AppSettings["HashKey"];
                    oPayment.HashIV = ConfigurationManager.AppSettings["HashIV"];
                    oPayment.MerchantID = ConfigurationManager.AppSettings["MerchantID"];

                    /* 基本參數 */
                    string hostname = Request.Url.Authority;
                    oPayment.Send.ReturnURL = $"{ApiDomain}/Home/GetPayResult";
                    oPayment.Send.OrderResultURL = $"{ApiDomain}/Home/GetPayResult";
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
                // 例外錯誤處理。 
                enErrors.Add(ex.Message);
            }
            finally
            {
                // 顯示錯誤訊息。 
                if (enErrors.Count() > 0)
                {
                    szHtml = string.Join("\\r\\n", enErrors);
                }
            }
            return Content(szHtml);
        }

        [HttpPost]
        public ActionResult GetPayResult(AllInOne oPayment)
        {
            List<string> enErrors = new List<string>();
            Hashtable htFeedback = null;
            string AppDomain = ConfigurationManager.AppSettings["AppDomain"];

            try
            {
                oPayment.HashKey = ConfigurationManager.AppSettings["HashKey"];
                oPayment.HashIV = ConfigurationManager.AppSettings["HashIV"];
                enErrors.AddRange(oPayment.CheckOutFeedback(ref htFeedback));


                if (enErrors.Count() == 0)
                {
                    int OrderId = (int)Session["OrderId"];
                    //return Redirect($"{AppDomain}/OrderForm/CheckPayResult/?OrderId={OrderId}&PayResult={true}");
                    return Content("付款成功!");
                }
                else
                {
                    return Redirect($"{AppDomain}/OrderForm/CheckPayResult/?PaySuccess=false");
                }
            }
            catch (Exception)
            {
                return Redirect($"{AppDomain}/OrderForm/CheckPayResult/?PaySuccess=false");
            }
        }
    }
}
