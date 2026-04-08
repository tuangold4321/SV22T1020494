using SV22T1020494.Models.Security;

namespace SV22T1020494.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các phép xử lý dữ liệu liên quan đến tài khoản
    /// </summary>
    public interface IUserAccountRepository
    {
        /// <summary>
        /// Kiểm tra xem tên đăng nhập và mật khẩu có hợp lệ không
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns>
        /// Trả về thông tin của tài khoản nếu thông tin đăng nhập hợp lệ,
        /// ngược lại trả về null
        /// </returns>
        Task<UserAccount?> Authorize(string userName, string password);
        /// <summary>
        /// Lấy thông tin bản ghi nhân viên theo email (dùng để kiểm tra trạng thái và mật khẩu)
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        Task<SV22T1020494.Models.Security.EmployeeRecord?> GetByEmailAsync(string email);
        /// <summary>
        /// Đổi mật khẩu của tài khoản
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        Task<bool> ChangePasswordAsync(string userName, string password);
    }
}
