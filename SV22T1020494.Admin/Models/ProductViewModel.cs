using System.ComponentModel.DataAnnotations;

namespace SV22T1020494.Models
{
    public class Product
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int SupplierID { get; set; } // Khoá ngoại
        public int CategoryID { get; set; } // Khoá ngoại
        public string Unit { get; set; } = string.Empty; // Đơn vị tính
        public decimal Price { get; set; } // Giá
        public string Photo { get; set; } = string.Empty;
        public bool IsDiscontinued { get; set; } // True: Ngừng bán, False: Đang bán
    }

    // Model phụ dùng cho Dropdown - renamed to avoid conflict with Catalog.Category
    public class CategoryItem { public int CategoryID { get; set; } public string CategoryName { get; set; } = string.Empty; }

    // Supplier model used by views/controllers - renamed to avoid conflict with Partner.Supplier
    public class SupplierItem
    {
        public int SupplierID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
    }
    // Supplier đã có ở bài trước
}