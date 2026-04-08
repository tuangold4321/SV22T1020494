using System;
using System.ComponentModel.DataAnnotations;

namespace SV22T1020494.Models
{
    public class OrderViewModel
    {
        public int OrderID { get; set; }

        [Display(Name = "Khách hàng")]
        public string CustomerName { get; set; }

        [Display(Name = "Ngày lập")]
        public DateTime OrderTime { get; set; }

        [Display(Name = "Nhân viên phụ trách")]
        public string EmployeeName { get; set; }

        [Display(Name = "Ngày giao hàng")]
        public DateTime? ShippedTime { get; set; } // Có thể null nếu chưa giao

        [Display(Name = "Đơn vị vận chuyển")]
        public string ShipperName { get; set; }

        [Display(Name = "Trạng thái")]
        public int Status { get; set; }
        // Quy ước: 
        // 1: Chờ xử lý (New)
        // 2: Đã duyệt (Accepted)
        // 3: Đang giao hàng (Shipping)
        // 4: Hoàn tất (Finished)
        // -1: Đã hủy (Canceled)
    }
}