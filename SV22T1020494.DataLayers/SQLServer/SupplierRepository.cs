using SV22T1020494.DataLayers.Interfaces;
using SV22T1020494.Models.Common;
using SV22T1020494.Models.Partner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace SV22T1020494.DataLayers.SQLServer
{
    public class SupplierRepository : IGenericRepository<Supplier>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString">Connection string to database</param>
        public SupplierRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> AddAsync(Supplier data)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = "INSERT INTO Suppliers(SupplierName, ContactName, Province, Address, Phone, Email) VALUES(@name, @contact, @province, @address, @phone, @email); SELECT CAST(SCOPE_IDENTITY() AS int);";
            cmd.Parameters.AddWithValue("@name", data.SupplierName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@contact", data.ContactName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@province", data.Province ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@address", data.Address ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@phone", data.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@email", data.Email ?? (object)DBNull.Value);

            await cn.OpenAsync();
            var id = await cmd.ExecuteScalarAsync();
            return id == null ? 0 : Convert.ToInt32(id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = "DELETE FROM Suppliers WHERE SupplierID = @id";
            cmd.Parameters.AddWithValue("@id", id);
            await cn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<Supplier?> GetAsync(int id)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT SupplierID, SupplierName, ContactName, Province, Address, Phone, Email FROM Suppliers WHERE SupplierID = @id";
            cmd.Parameters.AddWithValue("@id", id);
            await cn.OpenAsync();

            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                return new Supplier
                {
                    SupplierID = r.GetInt32(r.GetOrdinal("SupplierID")),
                    SupplierName = r.IsDBNull(r.GetOrdinal("SupplierName")) ? string.Empty : r.GetString(r.GetOrdinal("SupplierName")),
                    ContactName = r.IsDBNull(r.GetOrdinal("ContactName")) ? string.Empty : r.GetString(r.GetOrdinal("ContactName")),
                    Province = r.IsDBNull(r.GetOrdinal("Province")) ? null : r.GetString(r.GetOrdinal("Province")),
                    Address = r.IsDBNull(r.GetOrdinal("Address")) ? null : r.GetString(r.GetOrdinal("Address")),
                    Phone = r.IsDBNull(r.GetOrdinal("Phone")) ? null : r.GetString(r.GetOrdinal("Phone")),
                    Email = r.IsDBNull(r.GetOrdinal("Email")) ? null : r.GetString(r.GetOrdinal("Email"))
                };
            }

            return null;
        }

        public async Task<bool> IsUsed(int id)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            // Check Products table for reference to supplier
            cmd.CommandText = "SELECT TOP 1 1 FROM Products WHERE SupplierID = @id";
            cmd.Parameters.AddWithValue("@id", id);
            await cn.OpenAsync();
            var obj = await cmd.ExecuteScalarAsync();
            return obj != null;
        }

        public async Task<PagedResult<Supplier>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Supplier>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = 0,
                DataItems = new List<Supplier>()
            };

            using var cn = new SqlConnection(_connectionString);
            var cmdCount = cn.CreateCommand();

            var where = string.Empty;
            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                where = "WHERE SupplierName LIKE @search OR ContactName LIKE @search OR Email LIKE @search OR Phone LIKE @search";
                cmdCount.Parameters.AddWithValue("@search", "%" + input.SearchValue + "%");
            }

            cmdCount.CommandText = $"SELECT COUNT(*) FROM Suppliers {where}";

            await cn.OpenAsync();
            var totalObj = await cmdCount.ExecuteScalarAsync();
            result.RowCount = totalObj == null ? 0 : Convert.ToInt32(totalObj);

            if (input.PageSize == 0)
            {
                var cmdAll = cn.CreateCommand();
                cmdAll.CommandText = $"SELECT SupplierID, SupplierName, ContactName, Province, Address, Phone, Email FROM Suppliers {where} ORDER BY SupplierName";
                foreach (SqlParameter p in cmdCount.Parameters)
                    cmdAll.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));

                using var r = await cmdAll.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    result.DataItems.Add(new Supplier
                    {
                        SupplierID = r.GetInt32(r.GetOrdinal("SupplierID")),
                        SupplierName = r.IsDBNull(r.GetOrdinal("SupplierName")) ? string.Empty : r.GetString(r.GetOrdinal("SupplierName")),
                        ContactName = r.IsDBNull(r.GetOrdinal("ContactName")) ? string.Empty : r.GetString(r.GetOrdinal("ContactName")),
                        Province = r.IsDBNull(r.GetOrdinal("Province")) ? null : r.GetString(r.GetOrdinal("Province")),
                        Address = r.IsDBNull(r.GetOrdinal("Address")) ? null : r.GetString(r.GetOrdinal("Address")),
                        Phone = r.IsDBNull(r.GetOrdinal("Phone")) ? null : r.GetString(r.GetOrdinal("Phone")),
                        Email = r.IsDBNull(r.GetOrdinal("Email")) ? null : r.GetString(r.GetOrdinal("Email"))
                    });
                }

                return result;
            }

            if (result.RowCount == 0)
                return result;

            var offset = (input.Page - 1) * input.PageSize;
            var cmd = cn.CreateCommand();
            cmd.CommandText = $@"SELECT SupplierID, SupplierName, ContactName, Province, Address, Phone, Email
FROM Suppliers
{where}
ORDER BY SupplierName
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
            foreach (SqlParameter p in cmdCount.Parameters)
                cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
            cmd.Parameters.AddWithValue("@offset", offset);
            cmd.Parameters.AddWithValue("@pageSize", input.PageSize);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.DataItems.Add(new Supplier
                {
                    SupplierID = reader.GetInt32(reader.GetOrdinal("SupplierID")),
                    SupplierName = reader.IsDBNull(reader.GetOrdinal("SupplierName")) ? string.Empty : reader.GetString(reader.GetOrdinal("SupplierName")),
                    ContactName = reader.IsDBNull(reader.GetOrdinal("ContactName")) ? string.Empty : reader.GetString(reader.GetOrdinal("ContactName")),
                    Province = reader.IsDBNull(reader.GetOrdinal("Province")) ? null : reader.GetString(reader.GetOrdinal("Province")),
                    Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString(reader.GetOrdinal("Address")),
                    Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
                    Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email"))
                });
            }

            return result;
        }

        public async Task<bool> UpdateAsync(Supplier data)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = "UPDATE Suppliers SET SupplierName = @name, ContactName = @contact, Province = @province, Address = @address, Phone = @phone, Email = @email WHERE SupplierID = @id";
            cmd.Parameters.AddWithValue("@name", data.SupplierName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@contact", data.ContactName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@province", data.Province ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@address", data.Address ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@phone", data.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@email", data.Email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@id", data.SupplierID);
            await cn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}
