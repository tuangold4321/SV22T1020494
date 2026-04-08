using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020494.DataLayers.Interfaces;
using SV22T1020494.Models.Security;
using System.Threading.Tasks;

namespace SV22T1020494.DataLayers.SQLServer
{
    public class EmployeeAccountRepository : IUserAccountRepository
    {
        private readonly string _connectionString;

        public EmployeeAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<UserAccount?> Authorize(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            // Use Email column for login if UserName column doesn't exist in Employees table
            var sql = @"SELECT TOP 1 EmployeeID, FullName, Email, Photo, RoleNames FROM Employees WHERE Email = @userName AND Password = @password";
            var result = await connection.QuerySingleOrDefaultAsync(sql, new { userName, password });
            if (result == null) return null;

            var user = new UserAccount
            {
                UserId = result.EmployeeID.ToString(),
                UserName = userName,
                DisplayName = result.FullName ?? userName,
                Email = result.Email ?? string.Empty,
                Photo = result.Photo ?? string.Empty,
                RoleNames = result.RoleNames ?? "employee"
            };
            return user;
        }

        public async Task<SV22T1020494.Models.Security.EmployeeRecord?> GetByEmailAsync(string email)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"SELECT TOP 1 EmployeeID, FullName, Email, Photo, RoleNames, Password, IsWorking FROM Employees WHERE Email = @email";
            var result = await connection.QuerySingleOrDefaultAsync(sql, new { email });
            if (result == null) return null;

            var record = new SV22T1020494.Models.Security.EmployeeRecord
            {
                EmployeeID = result.EmployeeID,
                FullName = result.FullName ?? string.Empty,
                Email = result.Email ?? string.Empty,
                Photo = result.Photo ?? string.Empty,
                RoleNames = result.RoleNames ?? string.Empty,
                Password = result.Password ?? string.Empty,
                IsWorking = result.IsWorking ?? true
            };
            return record;
        }

        public async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            // Update by Email as Employees table uses Email (not UserName)
            var sql = "UPDATE Employees SET Password = @password WHERE Email = @userName";
            var affected = await connection.ExecuteAsync(sql, new { password, userName });
            return affected > 0;
        }
    }
}
