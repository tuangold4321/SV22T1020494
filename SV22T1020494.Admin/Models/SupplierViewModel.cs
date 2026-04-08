using System.ComponentModel.DataAnnotations;

namespace SV22T1020494.Models
{
    public class SupplierViewModel
    {
        public int SupplierID { get; set; }

        [Required(ErrorMessage = "Tên nhà cung cấp không được để trống")]
        [Display(Name = "Tên nhà cung cấp")]
        public string SupplierName { get; set; } = string.Empty;

        [Display(Name = "Tên giao dịch")]
        public string? ContactName { get; set; } // Thêm ? nếu muốn không bắt buộc

        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; } // Thêm ?

        [Display(Name = "Tỉnh/Thành")]
        public string? City { get; set; } // Thêm ?

        [Display(Name = "Quốc gia")]
        public string? Country { get; set; } // Thêm ?

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại cố định")]
        [Display(Name = "Điện thoại")]
        public string Phone { get; set; } = string.Empty;

        // --- QUAN TRỌNG: Thêm dấu ? vào sau string ---
        [Display(Name = "Di động")]
        public string? Mobile { get; set; }

        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; } // Thêm ? nếu muốn email là tùy chọn
    }
}