using System.Collections.Generic;

namespace SV22T1020494.Shop.Models
{
    /// <summary>
    /// Các chức năng trong giỏ hàng
    /// </summary>
    public class CartViewModel
    {
        /// <summary>
        /// Danh sách item trong giỏ
        /// </summary>
        public List<OrderItemRequest> Items { get; set; } = new List<OrderItemRequest>();

        /// <summary>
        /// Tổng tiền tạm tính (chưa bao gồm phí vận chuyển nếu có).
        /// </summary>

        public decimal Subtotal { get; set; }

        /// <summary>
        /// Tỉnh/thành giao hàng
        /// </summary>
        public string? Province { get; set; }

        /// <summary>
        /// Địa chỉ giao hàng chi tiết
        /// </summary>
        public string? Address { get; set; }
    }
}
