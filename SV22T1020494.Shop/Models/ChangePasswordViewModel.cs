namespace SV22T1020494.Shop.Models
{
    /// <summary>
    /// Chức năng đổi mật khẩu
    /// </summary>
    public class ChangePasswordViewModel
    {
        /// <summary>
        /// Mật khẩu hiện tại
        /// </summary>
        public string OldPassword { get; set; } = string.Empty;

        /// <summary>
        /// Mật khẩu mới
        /// </summary>
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Xác nhận mật khẩu mới
        /// </summary>
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
