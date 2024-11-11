namespace WatchStore.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Cart")]
    public class Cart
    {
        [Key]
        public int CartId { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedDate { get; set; }
        public virtual ICollection<CartDetail> CartDetails { get; set; }
        public Cart()
        {
            CartDetails = new List<CartDetail>();
        }

    }
}