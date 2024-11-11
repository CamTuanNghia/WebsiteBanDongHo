using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WatchStore.Models
{
    public class CartViewModel
    {
        public CartViewModel()
        {
            CartDetails = new List<CartDetail>();
        }

        public Cart Cart { get; set; }
        public List<CartDetail> CartDetails { get; set; }
    }
}