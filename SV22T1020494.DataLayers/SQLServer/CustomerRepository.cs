using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020494.DataLayers.Interfaces;
using SV22T1020494.Models.Common;
using SV22T1020494.Models.Partner;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020494.DataLayers.SQLServer
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly string _connectionString;

        // Simple in-memory cache to speed up repeated GetAsync calls
        // Key: CustomerID, Value: Customer (null entries are stored as null)
        private static readonly ConcurrentDictionary<int, Customer?> _cache = new();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString">Connection string to database</param>
        public CustomerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<PagedResult<Customer>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Customer>
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var whereBuilder = new StringBuilder("WHERE 1=1");
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                var s = $"%{input.SearchValue}%";
                whereBuilder.Append(" AND (CustomerName LIKE @s OR ContactName LIKE @s OR Email LIKE @s OR Address LIKE @s)");
                parameters.Add("s", s);
            }

            // Count total rows
            var countSql = $"SELECT COUNT(1) FROM Customers {whereBuilder}";
            result.RowCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            // If PageSize == 0 return all
            if (input.PageSize == 0)
            {
                var sqlAll = $"SELECT CustomerID, CustomerName, ContactName, Province, Address, Phone, Email, IsLocked FROM Customers {whereBuilder} ORDER BY CustomerID";
                var items = await connection.QueryAsync<Customer>(sqlAll, parameters);
                result.DataItems = items.ToList();
                return result;
            }

            // Pagination
            var offset = (input.Page - 1) * input.PageSize;
            var sql = $@"
SELECT CustomerID, CustomerName, ContactName, Province, Address, Phone, Email, IsLocked
FROM Customers
{whereBuilder}
ORDER BY CustomerID
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
            parameters.Add("offset", offset);
            parameters.Add("pageSize", input.PageSize);

            var pageItems = await connection.QueryAsync<Customer>(sql, parameters);
            result.DataItems = pageItems.ToList();

            return result;
        }

        public async Task<Customer?> GetAsync(int id)
        {
            // Return cached value if available
            if (_cache.TryGetValue(id, out var cached))
            {
                return cached;
            }

            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT CustomerID, CustomerName, ContactName, Province, Address, Phone, Email, IsLocked FROM Customers WHERE CustomerID = @id";
            var item = await connection.QuerySingleOrDefaultAsync<Customer>(sql, new { id });

            // Store result (including null) in cache for subsequent calls
            _cache.TryAdd(id, item);

            return item;
        }

        public async Task<int> AddAsync(Customer data)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"INSERT INTO Customers (CustomerName, ContactName, Province, Address, Phone, Email, IsLocked)
OUTPUT INSERTED.CustomerID
VALUES (@CustomerName, @ContactName, @Province, @Address, @Phone, @Email, @IsLocked)";
            var id = await connection.ExecuteScalarAsync<int>(sql, new
            {
                data.CustomerName,
                data.ContactName,
                data.Province,
                data.Address,
                data.Phone,
                data.Email,
                data.IsLocked
            });

            // Add to cache
            _cache.TryAdd(id, new Customer
            {
                CustomerID = id,
                CustomerName = data.CustomerName,
                ContactName = data.ContactName,
                Province = data.Province,
                Address = data.Address,
                Phone = data.Phone,
                Email = data.Email,
                IsLocked = data.IsLocked
            });

            return id;
        }

        public async Task<bool> UpdateAsync(Customer data)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"UPDATE Customers SET CustomerName = @CustomerName, ContactName = @ContactName, Province = @Province, Address = @Address, Phone = @Phone, Email = @Email, IsLocked = @IsLocked
WHERE CustomerID = @CustomerID";
            var affected = await connection.ExecuteAsync(sql, new
            {
                data.CustomerName,
                data.ContactName,
                data.Province,
                data.Address,
                data.Phone,
                data.Email,
                data.IsLocked,
                data.CustomerID
            });

            if (affected > 0)
            {
                // Update cache
                _cache.AddOrUpdate(data.CustomerID, data, (k, old) => data);
            }

            return affected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "DELETE FROM Customers WHERE CustomerID = @id";
            var affected = await connection.ExecuteAsync(sql, new { id });

            if (affected > 0)
            {
                // Remove from cache
                _cache.TryRemove(id, out _);
            }

            return affected > 0;
        }

        public async Task<bool> IsUsed(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            // Check Orders table for related data
            var sql = "SELECT COUNT(1) FROM Orders WHERE CustomerID = @id";
            var count = await connection.ExecuteScalarAsync<int>(sql, new { id });
            return count > 0;
        }

        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = new SqlConnection(_connectionString);
            if (id == 0)
            {
                var sql = "SELECT COUNT(1) FROM Customers WHERE Email = @email";
                var count = await connection.ExecuteScalarAsync<int>(sql, new { email });
                return count == 0;
            }
            else
            {
                var sql = "SELECT COUNT(1) FROM Customers WHERE Email = @email AND CustomerID <> @id";
                var count = await connection.ExecuteScalarAsync<int>(sql, new { email, id });
                return count == 0;
            }
        }
    }
}
