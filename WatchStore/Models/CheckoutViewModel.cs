using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WatchStore.Models
{
    public class CheckoutViewModel
    {
        public Cart Cart { get; set; }
        public MUser User { get; set; }
    }
}