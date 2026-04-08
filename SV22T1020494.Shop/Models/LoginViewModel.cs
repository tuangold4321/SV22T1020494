namespace SV22T1020494.Shop.Models
{
    /// <summary>
    /// Trang đăng nhập
    /// </summary>
    public class LoginViewModel
    {
        /// <summary>
        /// Email để đăng nhập
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Mật khẩu
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Trở về sau khi đăng nhập thành công (nếu có)
        /// </summary>
        public string? ReturnUrl { get; set; }
    }
}
