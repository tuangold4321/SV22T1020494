using SV22T1020494.DataLayers.Interfaces;
using SV22T1020494.Models.Catalog;
using SV22T1020494.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace SV22T1020494.DataLayers.SQLServer
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly string _connectionString;
        public CategoryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> AddAsync(Category data)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = "INSERT INTO Categories(CategoryName, Description) VALUES(@name, @desc); SELECT CAST(SCOPE_IDENTITY() AS int);";
            cmd.Parameters.AddWithValue("@name", data.CategoryName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@desc", data.Description ?? (object)DBNull.Value);

            await cn.OpenAsync();
            var id = await cmd.ExecuteScalarAsync();
            return id == null ? 0 : Convert.ToInt32(id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = "DELETE FROM Categories WHERE CategoryID = @id";
            cmd.Parameters.AddWithValue("@id", id);
            await cn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<Category?> GetAsync(int id)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT CategoryID, CategoryName, Description FROM Categories WHERE CategoryID = @id";
            cmd.Parameters.AddWithValue("@id", id);
            await cn.OpenAsync();

            using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    return new Category
                    {
                        CategoryID = r.GetInt32(r.GetOrdinal("CategoryID")),
                        CategoryName = r.IsDBNull(r.GetOrdinal("CategoryName")) ? string.Empty : r.GetString(r.GetOrdinal("CategoryName")),
                        Description = r.IsDBNull(r.GetOrdinal("Description")) ? null : r.GetString(r.GetOrdinal("Description"))
                    };
                }

            return null;
        }

        public async Task<bool> IsUsed(int id)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            // Check Products table for reference (common schema)
            cmd.CommandText = "SELECT TOP 1 1 FROM Products WHERE CategoryID = @id";
            cmd.Parameters.AddWithValue("@id", id);
            await cn.OpenAsync();
            var obj = await cmd.ExecuteScalarAsync();
            return obj != null;
        }

        public async Task<PagedResult<Category>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Category>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = 0,
                DataItems = new List<Category>()
            };

            using var cn = new SqlConnection(_connectionString);
            var cmdCount = cn.CreateCommand();

            var where = "";
            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                where = "WHERE CategoryName LIKE @search OR Description LIKE @search";
                cmdCount.Parameters.AddWithValue("@search", "%" + input.SearchValue + "%");
            }

            cmdCount.CommandText = $"SELECT COUNT(*) FROM Categories {where}";

            await cn.OpenAsync();
            var totalObj = await cmdCount.ExecuteScalarAsync();
            result.RowCount = totalObj == null ? 0 : Convert.ToInt32(totalObj);

            // If PageSize == 0, return all
            if (input.PageSize == 0)
            {
                var cmdAll = cn.CreateCommand();
                cmdAll.CommandText = $"SELECT CategoryID, CategoryName, Description FROM Categories {where} ORDER BY CategoryName";
                foreach (SqlParameter p in cmdCount.Parameters)
                    cmdAll.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));

                using var r = await cmdAll.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    result.DataItems.Add(new Category
                    {
                        CategoryID = r.GetInt32(r.GetOrdinal("CategoryID")),
                        CategoryName = r.IsDBNull(r.GetOrdinal("CategoryName")) ? string.Empty : r.GetString(r.GetOrdinal("CategoryName")),
                        Description = r.IsDBNull(r.GetOrdinal("Description")) ? null : r.GetString(r.GetOrdinal("Description"))
                    });
                }
                return result;
            }

            if (result.RowCount == 0)
            {
                // no data
                return result;
            }

            var offset = (input.Page - 1) * input.PageSize;
            var cmd = cn.CreateCommand();
            cmd.CommandText = $@"SELECT CategoryID, CategoryName, Description
FROM Categories
{where}
ORDER BY CategoryName
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
            foreach (SqlParameter p in cmdCount.Parameters)
                cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
            cmd.Parameters.AddWithValue("@offset", offset);
            cmd.Parameters.AddWithValue("@pageSize", input.PageSize);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.DataItems.Add(new Category
                {
                    CategoryID = reader.GetInt32(reader.GetOrdinal("CategoryID")),
                    CategoryName = reader.IsDBNull(reader.GetOrdinal("CategoryName")) ? string.Empty : reader.GetString(reader.GetOrdinal("CategoryName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description"))
                });
            }

            return result;
        }

        public async Task<bool> UpdateAsync(Category data)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = "UPDATE Categories SET CategoryName = @name, Description = @desc WHERE CategoryID = @id";
            cmd.Parameters.AddWithValue("@name", data.CategoryName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@desc", data.Description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@id", data.CategoryID);
            await cn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}
