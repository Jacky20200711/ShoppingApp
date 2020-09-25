using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OpayApi.Models
{
    [Serializable]
    public class CartItem
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Price { get; set; }

        public int Quantity { get; set; }

        public int Amount
        {
            get
            {
                return Price * Quantity;
            }
        }
    }
}