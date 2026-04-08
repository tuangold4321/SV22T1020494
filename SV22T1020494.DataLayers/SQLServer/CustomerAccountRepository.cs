using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020494.DataLayers.Interfaces;
using SV22T1020494.Models.Security;
using System.Threading.Tasks;

namespace SV22T1020494.DataLayers.SQLServer
{
    public class CustomerAccountRepository : IUserAccountRepository
    {
        private readonly string _connectionString;

        public CustomerAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<UserAccount?> Authorize(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"SELECT TOP 1 CustomerID, CustomerName, Email FROM Customers WHERE Email = @userName AND Password = @password";
            var result = await connection.QuerySingleOrDefaultAsync(sql, new { userName, password });
            if (result == null) return null;

            var user = new UserAccount
            {
                UserId = result.CustomerID.ToString(),
                UserName = result.Email ?? userName,
                DisplayName = result.CustomerName ?? (result.Email ?? string.Empty),
                Email = result.Email ?? string.Empty,
                Photo = string.Empty,
                RoleNames = "Customer"
            };
            return user;
        }

        public async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "UPDATE Customers SET Password = @password WHERE Email = @userName";
            var affected = await connection.ExecuteAsync(sql, new { password, userName });
            return affected > 0;
        }

        /// <summary>
        /// Lấy thông tin customer bằng Email (cho login)
        /// </summary>
        public async Task<dynamic?> GetCustomerByEmailAsync(string email)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT TOP 1 CustomerID, CustomerName, Email, Password, IsLocked FROM Customers WHERE Email = @email";
            return await connection.QuerySingleOrDefaultAsync(sql, new { email });
        }

        /// <summary>
        /// Lấy thông tin customer bằng CustomerID
        /// </summary>
        public async Task<dynamic?> GetCustomerByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT CustomerID, CustomerName, ContactName, Province, Address, Phone, Email, Password FROM Customers WHERE CustomerID = @id";
            return await connection.QuerySingleOrDefaultAsync(sql, new { id });
        }

        /// <summary>
        /// Kiểm tra email đã tồn tại chưa
        /// </summary>
        public async Task<bool> EmailExistsAsync(string email)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT COUNT(1) FROM Customers WHERE Email = @email";
            var count = await connection.ExecuteScalarAsync<int>(sql, new { email });
            return count > 0;
        }

        /// <summary>
        /// Đăng ký
        /// </summary>
        public async Task<int> CreateCustomerAsync(string customerName, string? contactName, string province, string address, string phone, string email, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                   INSERT INTO Customers (CustomerName, ContactName, Province, Address, Phone, Email, Password, IsLocked)
                   OUTPUT INSERTED.CustomerID
                   VALUES (@CustomerName, @ContactName, @Province, @Address, @Phone, @Email, @Password, 0)";
            var contact = string.IsNullOrWhiteSpace(contactName) ? customerName ?? string.Empty : contactName;
            var id = await connection.ExecuteScalarAsync<int>(sql, new
            {
                CustomerName = customerName,
                ContactName = contact,
                Province = province,
                Address = address,
                Phone = phone,
                Email = email,
                Password = password
            });
            return id;
        }

        /// <summary>
        /// Cập nhật thông tin customer
        /// </summary>
        public async Task<bool> UpdateCustomerAsync(int customerId, string customerName, string? contactName, string phone, string province, string address)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"UPDATE Customers SET CustomerName=@customerName, ContactName=@contactName, Phone=@phone, Province=@province, Address=@address WHERE CustomerID=@id";
            var contact = string.IsNullOrWhiteSpace(contactName) ? customerName ?? string.Empty : contactName;
            var affected = await connection.ExecuteAsync(sql, new { customerName, contactName = contact, phone, province, address, id = customerId });
            return affected > 0;
        }

        /// <summary>
        /// Lấy mật khẩu hiện tại của customer
        /// </summary>
        public async Task<string?> GetCustomerPasswordAsync(int customerId)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT Password FROM Customers WHERE CustomerID=@id";
            return await connection.ExecuteScalarAsync<string?>(sql, new { id = customerId });
        }

        /// <summary>
        /// Lấy khách hàng từ Email
        /// </summary>
        public async Task<int?> GetCustomerIdByEmailAsync(string email)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT TOP 1 CustomerID FROM Customers WHERE Email = @email";
            return await connection.ExecuteScalarAsync<int?>(sql, new { email });
        }

        public async Task<SV22T1020494.Models.Security.EmployeeRecord?> GetByEmailAsync(string email)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT TOP 1 CustomerID, CustomerName, Email, Password, IsLocked FROM Customers WHERE Email = @email";
            var result = await connection.QuerySingleOrDefaultAsync(sql, new { email });
            if (result == null) return null;

            var record = new SV22T1020494.Models.Security.EmployeeRecord
            {
                EmployeeID = result.CustomerID,
                FullName = result.CustomerName ?? string.Empty,
                Email = result.Email ?? string.Empty,
                Photo = string.Empty,
                RoleNames = "Customer",
                Password = result.Password ?? string.Empty,
                IsWorking = !(result.IsLocked ?? false)
            };
            return record;
        }
    }
}
