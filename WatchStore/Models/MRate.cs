namespace WatchStore.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("ProductRate")]
    public class MRate
    {
        [Key]
        [Required]
        public int ID { get; set; }
        public int ProductID { get; set; }
        public int UserID { get; set; }
        public Double Rate { get; set; }
        public string Comment { get; set; }
        public string UName { get; set; }
        public DateTime CreateAt { get; set; }

    }
}