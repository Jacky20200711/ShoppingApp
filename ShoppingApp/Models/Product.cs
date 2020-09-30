using System;
using System.ComponentModel.DataAnnotations;

namespace ShoppingApp.Models
{
    public class Product
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public int Price { get; set; }

        [Required]
        public DateTime PublishDate { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public string DefaultImageURL { get; set; }
    }
}