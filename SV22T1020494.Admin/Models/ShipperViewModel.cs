using System.ComponentModel.DataAnnotations;

namespace SV22T1020494.Models
{
    public class ShipperViewModel
    {
        public int ShipperID { get; set; }

        [Required(ErrorMessage = "Tên công ty giao hàng không được để trống")]
        [Display(Name = "Tên công ty / Người giao hàng")]
        public string ShipperName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Display(Name = "Điện thoại")]
        public string Phone { get; set; } = string.Empty;
    }
}