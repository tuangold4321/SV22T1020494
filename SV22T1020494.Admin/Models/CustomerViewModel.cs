using System.ComponentModel.DataAnnotations;
using SV22T1020494.Models.Partner;

namespace SV22T1020494.Models
{
    // Do not inherit from domain Customer to avoid accidental recursive property access in view-model
    public class CustomerViewModel
    {
        public int CustomerID { get; set; }

        [Display(Name = "Tên khách hàng")]
        public string CustomerName { get; set; } = string.Empty;

        [Display(Name = "Tên giao dịch")]
        public string ContactName { get; set; } = string.Empty;

        [Display(Name = "Địa chỉ")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "Tỉnh/Thành")]
        public string Province { get; set; } = string.Empty;

        [Display(Name = "Quốc gia")]
        public string Country { get; set; } = string.Empty;

        [Display(Name = "Điện thoại")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        public bool IsLocked { get; set; } = false;
    }
}