using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // Dùng nếu muốn upload ảnh thật

namespace SV22T1020494.Models
{
    public class EmployeeViewModel
    {
        public int EmployeeID { get; set; }

        [Required(ErrorMessage = "Họ không được để trống")]
        [Display(Name = "Họ")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên không được để trống")]
        [Display(Name = "Tên")]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Chức vụ")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Phòng ban")]
        public string Department { get; set; } = string.Empty; // Managerial, Development...

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [Display(Name = "Ngày tham gia")]
        [DataType(DataType.Date)]
        public DateTime? HireDate { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Địa chỉ")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "Thành phố")]
        public string City { get; set; } = string.Empty;

        [Display(Name = "Quốc gia")]
        public string Country { get; set; } = string.Empty;

        [Display(Name = "Điện thoại")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Ảnh đại diện")]
        public string PhotoPath { get; set; } = string.Empty; // Đường dẫn ảnh

        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; } // True: Active, False: Inactive

        [Display(Name = "Vai trò")]
        public string Role { get; set; } = string.Empty; // Admin, Staff, Sale...

        // Property hỗ trợ hiển thị FullName
        public string FullName => $"{LastName} {FirstName}";
    }
}