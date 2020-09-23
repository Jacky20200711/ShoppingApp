using System;
using System.ComponentModel.DataAnnotations;

namespace ShoppingApp.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [StringLength(100, ErrorMessage = "輸入長度為2~100字", MinimumLength = 2)]
        public string Content { get; set; }

        public string UserName { get; set; }
        public DateTime CreateTime { get; set; }
        public int ProductId { get; set; }
    }
}