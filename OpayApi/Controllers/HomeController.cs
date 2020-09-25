using Newtonsoft.Json;
using OpayApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OpayApi.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult SendToOpay(string JsonString)
        {
            Cart currentCart = JsonConvert.DeserializeObject<Cart>(JsonString);

            string s = "";

            foreach(var cartItem in currentCart)
            {
                s += $"{cartItem.Id}, {cartItem.Name}, {cartItem.Price}, {cartItem.Quantity}<br>";
            }

            return Content(s);
        }
    }
}
