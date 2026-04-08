using SV22T1020494.DataLayers.Interfaces;
using SV22T1020494.DataLayers.SQLServer;
using SV22T1020494.Models.Security;
using System.Threading.Tasks;

namespace SV22T1020494.BusinessLayers
{
    /// <summary>
    /// D?ch v? liên quan ð?n b?o m?t (xác th?c/ð?i m?t kh?u)
    /// </summary>
    public static class SecurityDataService
    {
        private static readonly IUserAccountRepository userAccountDB;

        static SecurityDataService()
        {
            userAccountDB = new EmployeeAccountRepository(Configuration.ConnectionString);
        }

        /// <summary>
        /// Ki?m tra tên ðãng nh?p/m?t kh?u và tr? v? thông tin tài kho?n n?u h?p l?
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password">(ð? ðý?c bãm trý?c khi g?i n?u c?n)</param>
        /// <returns></returns>
        public static async Task<UserAccount?> EmployeeAuthorizeAsync(string userName, string password)
        {
            return await userAccountDB.Authorize(userName, password);
        }

        /// <summary>
        /// Authenticate and return detailed result (not found / locked / invalid password / success)
        /// </summary>
        public static async Task<SV22T1020494.Models.Security.AuthenticationResult> EmployeeAuthenticateAsync(string userName, string password)
        {
            var result = new SV22T1020494.Models.Security.AuthenticationResult();

            var record = await userAccountDB.GetByEmailAsync(userName);
            if (record == null)
            {
                result.Status = SV22T1020494.Models.Security.AuthenticationStatus.NotFound;
                return result;
            }

            if (!record.IsWorking)
            {
                result.Status = SV22T1020494.Models.Security.AuthenticationStatus.Locked;
                return result;
            }

            if (record.Password != password)
            {
                result.Status = SV22T1020494.Models.Security.AuthenticationStatus.InvalidPassword;
                return result;
            }

            // success - build UserAccount
            result.Status = SV22T1020494.Models.Security.AuthenticationStatus.Success;
            result.User = new UserAccount
            {
                UserId = record.EmployeeID.ToString(),
                UserName = record.Email,
                DisplayName = string.IsNullOrEmpty(record.FullName) ? record.Email : record.FullName,
                Email = record.Email ?? string.Empty,
                Photo = record.Photo ?? string.Empty,
                RoleNames = record.RoleNames ?? "employee"
            };
            return result;
        }

        /// <summary>
        /// Ð?i m?t kh?u cho tài kho?n nhân viên
        /// </summary>
        public static async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            return await userAccountDB.ChangePasswordAsync(userName, password);
        }
    }
}
