using System.Collections.Generic;

namespace SV22T1020494.Shop.Models
{
    /// <summary>
    /// ViewModel chứa dữ liệu liên quan đến việc tạo đơn hàng từ client.
    /// Bao gồm thông tin địa chỉ giao hàng và danh sách mục hàng.
    /// </summary>
    public class CreateOrderRequest
    {
        /// <summary> Tỉnh/Thành nơi giao hàng. </summary>
        public string Province { get; set; }

        /// <summary> Địa chỉ giao hàng chi tiết. </summary>
        public string Address { get; set; }

        /// <summary> Danh sách các mục hàng trong đơn. </summary>
        public List<OrderItemRequest> Items { get; set; }
    }

    /// <summary>
    /// Đại diện cho một mục hàng trong <see cref="CreateOrderRequest"/>.
    /// </summary>
    public class OrderItemRequest
    {
        /// <summary> Mã sản phẩm. </summary>
        public int ProductID { get; set; }

        /// <summary> Số lượng đặt. </summary>
        public int Quantity { get; set; }

        /// <summary> Giá bán được lưu khi đặt hàng. </summary>
        public decimal Price { get; set; }
    }
}
