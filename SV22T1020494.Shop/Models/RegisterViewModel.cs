namespace SV22T1020494.Shop.Models
{
    /// <summary>
    /// Chứa các trường cần thiết để tạo tài khoản khách hàng.
    /// </summary>
    public class RegisterViewModel
    {
        /// <summary> Tên khách hàng / Tên doanh nghiệp. </summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary> Tên liên hệ (tuỳ chọn). </summary>
        public string? ContactName { get; set; }

        /// <summary> Email dùng làm tên đăng nhập. </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary> Mật khẩu. </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary> Số điện thoại. </summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary> Tỉnh/Thành nơi giao hàng / địa chỉ (lấy từ dropdown). </summary>
        public string Province { get; set; } = string.Empty;

        /// <summary> Địa chỉ chi tiết (không bắt buộc). </summary>
        public string? Address { get; set; }
    }
}
