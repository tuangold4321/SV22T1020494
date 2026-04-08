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
    public class ShipperRepository : IGenericRepository<Shipper>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString">Connection string to database</param>
        public ShipperRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> AddAsync(Shipper data)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = "INSERT INTO Shippers(ShipperName, Phone) VALUES(@name, @phone); SELECT CAST(SCOPE_IDENTITY() AS int);";
            cmd.Parameters.AddWithValue("@name", data.ShipperName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@phone", data.Phone ?? (object)DBNull.Value);

            await cn.OpenAsync();
            var id = await cmd.ExecuteScalarAsync();
            return id == null ? 0 : Convert.ToInt32(id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = "DELETE FROM Shippers WHERE ShipperID = @id";
            cmd.Parameters.AddWithValue("@id", id);
            await cn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<Shipper?> GetAsync(int id)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT ShipperID, ShipperName, Phone FROM Shippers WHERE ShipperID = @id";
            cmd.Parameters.AddWithValue("@id", id);
            await cn.OpenAsync();

            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                return new Shipper
                {
                    ShipperID = r.GetInt32(r.GetOrdinal("ShipperID")),
                    ShipperName = r.IsDBNull(r.GetOrdinal("ShipperName")) ? string.Empty : r.GetString(r.GetOrdinal("ShipperName")),
                    Phone = r.IsDBNull(r.GetOrdinal("Phone")) ? null : r.GetString(r.GetOrdinal("Phone"))
                };
            }

            return null;
        }

        public async Task<bool> IsUsed(int id)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            // Check Orders table for reference to ShipperID if exists
            cmd.CommandText = "SELECT TOP 1 1 FROM Orders WHERE ShipperID = @id";
            cmd.Parameters.AddWithValue("@id", id);
            await cn.OpenAsync();
            var obj = await cmd.ExecuteScalarAsync();
            return obj != null;
        }

        public async Task<PagedResult<Shipper>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Shipper>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = 0,
                DataItems = new List<Shipper>()
            };

            using var cn = new SqlConnection(_connectionString);
            var cmdCount = cn.CreateCommand();

            var where = string.Empty;
            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                where = "WHERE ShipperName LIKE @search OR Phone LIKE @search";
                cmdCount.Parameters.AddWithValue("@search", "%" + input.SearchValue + "%");
            }

            cmdCount.CommandText = $"SELECT COUNT(*) FROM Shippers {where}";

            await cn.OpenAsync();
            var totalObj = await cmdCount.ExecuteScalarAsync();
            result.RowCount = totalObj == null ? 0 : Convert.ToInt32(totalObj);

            if (input.PageSize == 0)
            {
                var cmdAll = cn.CreateCommand();
                cmdAll.CommandText = $"SELECT ShipperID, ShipperName, Phone FROM Shippers {where} ORDER BY ShipperName";
                foreach (SqlParameter p in cmdCount.Parameters)
                    cmdAll.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));

                using var r = await cmdAll.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    result.DataItems.Add(new Shipper
                    {
                        ShipperID = r.GetInt32(r.GetOrdinal("ShipperID")),
                        ShipperName = r.IsDBNull(r.GetOrdinal("ShipperName")) ? string.Empty : r.GetString(r.GetOrdinal("ShipperName")),
                        Phone = r.IsDBNull(r.GetOrdinal("Phone")) ? null : r.GetString(r.GetOrdinal("Phone"))
                    });
                }

                return result;
            }

            if (result.RowCount == 0)
                return result;

            var offset = (input.Page - 1) * input.PageSize;
            var cmd = cn.CreateCommand();
            cmd.CommandText = $@"SELECT ShipperID, ShipperName, Phone
FROM Shippers
{where}
ORDER BY ShipperName
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
            foreach (SqlParameter p in cmdCount.Parameters)
                cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
            cmd.Parameters.AddWithValue("@offset", offset);
            cmd.Parameters.AddWithValue("@pageSize", input.PageSize);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.DataItems.Add(new Shipper
                {
                    ShipperID = reader.GetInt32(reader.GetOrdinal("ShipperID")),
                    ShipperName = reader.IsDBNull(reader.GetOrdinal("ShipperName")) ? string.Empty : reader.GetString(reader.GetOrdinal("ShipperName")),
                    Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone"))
                });
            }

            return result;
        }

        public async Task<bool> UpdateAsync(Shipper data)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = "UPDATE Shippers SET ShipperName = @name, Phone = @phone WHERE ShipperID = @id";
            cmd.Parameters.AddWithValue("@name", data.ShipperName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@phone", data.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@id", data.ShipperID);
            await cn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}
