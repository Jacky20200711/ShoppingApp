using System;
using System.Collections.Generic;
using System.Linq;

namespace ShoppingApp.Models
{
    [Serializable]
    public class Cart : ICollection<CartItem>
    {
        public List<CartItem> cartItems;

        //建構子
        public Cart()
        {
            cartItems = new List<CartItem>();
        }

        //取得購物車內商品總數
        public int Count
        {
            get
            {
                return cartItems.Count;
            }
        }

        //取得購物車內的商品總價
        public int TotalAmount
        {
            get
            {
                return cartItems.Sum(p => p.Amount);
            }
        }

        public bool IsReadOnly => ((ICollection<CartItem>)cartItems).IsReadOnly;

        public void Add(CartItem item)
        {
            ((ICollection<CartItem>)cartItems).Add(item);
        }

        public void Clear()
        {
            ((ICollection<CartItem>)cartItems).Clear();
        }

        public bool Contains(CartItem item)
        {
            return ((ICollection<CartItem>)cartItems).Contains(item);
        }

        public void CopyTo(CartItem[] array, int arrayIndex)
        {
            ((ICollection<CartItem>)cartItems).CopyTo(array, arrayIndex);
        }

        public bool Remove(CartItem item)
        {
            return ((ICollection<CartItem>)cartItems).Remove(item);
        }

        #region IEnumerator

        IEnumerator<CartItem> IEnumerable<CartItem>.GetEnumerator()
        {
            return cartItems.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return cartItems.GetEnumerator();
        }

        #endregion
    }
}