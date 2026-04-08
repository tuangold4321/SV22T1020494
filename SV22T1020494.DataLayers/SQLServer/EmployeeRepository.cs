using SV22T1020494.DataLayers.Interfaces;
using SV22T1020494.Models.Common;
using SV22T1020494.Models.HR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace SV22T1020494.DataLayers.SQLServer
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly string _connectionString;
        public EmployeeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> AddAsync(Employee data)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = "INSERT INTO Employees(FullName, BirthDate, Address, Phone, Email, Password, Photo, IsWorking, RoleNames) VALUES(@fullName, @birthDate, @address, @phone, @email, @password, @photo, @isWorking, @roleNames); SELECT CAST(SCOPE_IDENTITY() AS int);";
            cmd.Parameters.AddWithValue("@fullName", data.FullName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@birthDate", data.BirthDate.HasValue ? (object)data.BirthDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@address", data.Address ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@phone", data.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@email", data.Email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@password", data.Password ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@photo", data.Photo ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@isWorking", data.IsWorking.HasValue ? (object)(data.IsWorking.Value ? 1 : 0) : DBNull.Value);
            cmd.Parameters.AddWithValue("@roleNames", data.RoleNames ?? (object)DBNull.Value);

            await cn.OpenAsync();
            var id = await cmd.ExecuteScalarAsync();
            return id == null ? 0 : Convert.ToInt32(id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = "DELETE FROM Employees WHERE EmployeeID = @id";
            cmd.Parameters.AddWithValue("@id", id);
            await cn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<Employee?> GetAsync(int id)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT EmployeeID, FullName, BirthDate, Address, Phone, Email, Password, Photo, IsWorking, RoleNames FROM Employees WHERE EmployeeID = @id";
            cmd.Parameters.AddWithValue("@id", id);
            await cn.OpenAsync();

            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                return new Employee
                {
                    EmployeeID = r.GetInt32(r.GetOrdinal("EmployeeID")),
                    FullName = r.IsDBNull(r.GetOrdinal("FullName")) ? string.Empty : r.GetString(r.GetOrdinal("FullName")),
                    BirthDate = r.IsDBNull(r.GetOrdinal("BirthDate")) ? null : r.GetDateTime(r.GetOrdinal("BirthDate")),
                    Address = r.IsDBNull(r.GetOrdinal("Address")) ? null : r.GetString(r.GetOrdinal("Address")),
                    Phone = r.IsDBNull(r.GetOrdinal("Phone")) ? null : r.GetString(r.GetOrdinal("Phone")),
                    Email = r.IsDBNull(r.GetOrdinal("Email")) ? string.Empty : r.GetString(r.GetOrdinal("Email")),
                    Password = r.IsDBNull(r.GetOrdinal("Password")) ? null : r.GetString(r.GetOrdinal("Password")),
                    Photo = r.IsDBNull(r.GetOrdinal("Photo")) ? null : r.GetString(r.GetOrdinal("Photo")),
                    IsWorking = r.IsDBNull(r.GetOrdinal("IsWorking")) ? null : (bool?)r.GetBoolean(r.GetOrdinal("IsWorking")),
                    RoleNames = r.IsDBNull(r.GetOrdinal("RoleNames")) ? null : r.GetString(r.GetOrdinal("RoleNames"))
                };
            }
            return null;
        }

        public async Task<bool> IsUsed(int id)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            // Check Orders or other tables referencing employees (adjust if needed)
            cmd.CommandText = "SELECT TOP 1 1 FROM Orders WHERE EmployeeID = @id";
            cmd.Parameters.AddWithValue("@id", id);
            await cn.OpenAsync();
            var obj = await cmd.ExecuteScalarAsync();
            return obj != null;
        }

        public async Task<PagedResult<Employee>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Employee>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = 0,
                DataItems = new List<Employee>()
            };

            using var cn = new SqlConnection(_connectionString);
            var cmdCount = cn.CreateCommand();

            var where = string.Empty;
            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                where = "WHERE FullName LIKE @search OR Email LIKE @search OR Phone LIKE @search";
                cmdCount.Parameters.AddWithValue("@search", "%" + input.SearchValue + "%");
            }

            cmdCount.CommandText = $"SELECT COUNT(*) FROM Employees {where}";
            await cn.OpenAsync();
            var totalObj = await cmdCount.ExecuteScalarAsync();
            result.RowCount = totalObj == null ? 0 : Convert.ToInt32(totalObj);

            if (input.PageSize == 0)
            {
                var cmdAll = cn.CreateCommand();
                cmdAll.CommandText = $"SELECT EmployeeID, FullName, BirthDate, Address, Phone, Email, Password, Photo, IsWorking, RoleNames FROM Employees {where} ORDER BY EmployeeID";
                foreach (SqlParameter p in cmdCount.Parameters)
                    cmdAll.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));

                using var r = await cmdAll.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    result.DataItems.Add(new Employee
                    {
                        EmployeeID = r.GetInt32(r.GetOrdinal("EmployeeID")),
                        FullName = r.IsDBNull(r.GetOrdinal("FullName")) ? string.Empty : r.GetString(r.GetOrdinal("FullName")),
                        BirthDate = r.IsDBNull(r.GetOrdinal("BirthDate")) ? null : r.GetDateTime(r.GetOrdinal("BirthDate")),
                        Address = r.IsDBNull(r.GetOrdinal("Address")) ? null : r.GetString(r.GetOrdinal("Address")),
                        Phone = r.IsDBNull(r.GetOrdinal("Phone")) ? null : r.GetString(r.GetOrdinal("Phone")),
                        Email = r.IsDBNull(r.GetOrdinal("Email")) ? string.Empty : r.GetString(r.GetOrdinal("Email")),
                        Password = r.IsDBNull(r.GetOrdinal("Password")) ? null : r.GetString(r.GetOrdinal("Password")),
                        Photo = r.IsDBNull(r.GetOrdinal("Photo")) ? null : r.GetString(r.GetOrdinal("Photo")),
                        IsWorking = r.IsDBNull(r.GetOrdinal("IsWorking")) ? null : (bool?)r.GetBoolean(r.GetOrdinal("IsWorking")),
                        RoleNames = r.IsDBNull(r.GetOrdinal("RoleNames")) ? null : r.GetString(r.GetOrdinal("RoleNames"))
                    });
                }
                return result;
            }

            if (result.RowCount == 0)
                return result;

            var offset = (input.Page - 1) * input.PageSize;
            var cmd = cn.CreateCommand();
            cmd.CommandText = $@"SELECT EmployeeID, FullName, BirthDate, Address, Phone, Email, Password, Photo, IsWorking, RoleNames
FROM Employees
{where}
ORDER BY EmployeeID
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
            foreach (SqlParameter p in cmdCount.Parameters)
                cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
            cmd.Parameters.AddWithValue("@offset", offset);
            cmd.Parameters.AddWithValue("@pageSize", input.PageSize);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.DataItems.Add(new Employee
                {
                    EmployeeID = reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                    FullName = reader.IsDBNull(reader.GetOrdinal("FullName")) ? string.Empty : reader.GetString(reader.GetOrdinal("FullName")),
                    BirthDate = reader.IsDBNull(reader.GetOrdinal("BirthDate")) ? null : reader.GetDateTime(reader.GetOrdinal("BirthDate")),
                    Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString(reader.GetOrdinal("Address")),
                    Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
                    Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : reader.GetString(reader.GetOrdinal("Email")),
                    Password = reader.IsDBNull(reader.GetOrdinal("Password")) ? null : reader.GetString(reader.GetOrdinal("Password")),
                    Photo = reader.IsDBNull(reader.GetOrdinal("Photo")) ? null : reader.GetString(reader.GetOrdinal("Photo")),
                    IsWorking = reader.IsDBNull(reader.GetOrdinal("IsWorking")) ? null : (bool?)reader.GetBoolean(reader.GetOrdinal("IsWorking")),
                    RoleNames = reader.IsDBNull(reader.GetOrdinal("RoleNames")) ? null : reader.GetString(reader.GetOrdinal("RoleNames"))
                });
            }

            return result;
        }

        public async Task<bool> UpdateAsync(Employee data)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = "UPDATE Employees SET FullName = @fullName, BirthDate = @birthDate, Address = @address, Phone = @phone, Email = @email, Photo = @photo, IsWorking = @isWorking, RoleNames = @roleNames WHERE EmployeeID = @id";
            cmd.Parameters.AddWithValue("@fullName", data.FullName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@birthdate", data.BirthDate.HasValue ? (object)data.BirthDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@address", data.Address ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@phone", data.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@email", data.Email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@photo", data.Photo ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@isWorking", data.IsWorking.HasValue ? (object)(data.IsWorking.Value ? 1 : 0) : DBNull.Value);
            cmd.Parameters.AddWithValue("@roleNames", data.RoleNames ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@id", data.EmployeeID);
            await cn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = id == 0 ? "SELECT COUNT(*) FROM Employees WHERE Email = @email" : "SELECT COUNT(*) FROM Employees WHERE Email = @email AND EmployeeID <> @id";
            cmd.Parameters.AddWithValue("@email", email ?? string.Empty);
            if (id != 0) cmd.Parameters.AddWithValue("@id", id);
            await cn.OpenAsync();
            var obj = await cmd.ExecuteScalarAsync();
            var count = obj == null ? 0 : Convert.ToInt32(obj);
            return count == 0;
        }
    }
}
