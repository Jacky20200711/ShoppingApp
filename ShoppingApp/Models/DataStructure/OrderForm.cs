using System;
using System.ComponentModel.DataAnnotations;

namespace ShoppingApp.Models
{
    public class OrderForm
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "此欄位不能為空")]
        [Display(Name = "收貨人姓名")]
        [StringLength(30, ErrorMessage = "輸入長度為2~30字", MinimumLength = 2)]
        public string ReceiverName { get; set; }

        [Required(ErrorMessage = "此欄位不能為空")]
        [Display(Name = "收貨人電話")]
        [RegularExpression(@"^[0-9''-'\d]{10,11}$", ErrorMessage = "輸入內容必須為10~11個數字")]
        public string ReceiverPhone { get; set; }

        [Required(ErrorMessage = "此欄位不能為空")]
        [Display(Name = "收貨人住址")]
        [StringLength(60, ErrorMessage = "輸入長度為8~60字", MinimumLength = 8)]
        public string ReceiverAddress { get; set; }

        [Display(Name = "下單者郵件")]
        public string SenderEmail { get; set; }

        [Display(Name = "建立時間")]
        public DateTime CreateTime { get; set; }

        [Display(Name = "總金額")]
        public int TotalAmount { get; set; }

        [Display(Name = "已付款")]
        public string CheckOut { get; set; }
    }
}