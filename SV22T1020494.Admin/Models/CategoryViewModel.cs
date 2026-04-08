using System.ComponentModel.DataAnnotations;

namespace SV22T1020494.Models
{
    public class CategoryViewModel
    {
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Tên loại hàng không được để trống")]
        [Display(Name = "Tên loại hàng")]
        public string CategoryName { get; set; } = string.Empty;

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }
    }
}