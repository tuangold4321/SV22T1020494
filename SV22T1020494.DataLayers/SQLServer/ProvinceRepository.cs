using SV22T1020494.DataLayers.Interfaces;
using SV22T1020494.Models.DataDictionary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;

namespace SV22T1020494.DataLayers.SQLServer
{
    public class ProvinceRepository : IDataDictionaryRepository<Province>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString">Connection string to database</param>
        public ProvinceRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<Province>> ListAsync()
        {
            const string sql = @"SELECT ProvinceName FROM Provinces ORDER BY ProvinceName";
            using (var conn = new SqlConnection(_connectionString))
            {
                var items = await conn.QueryAsync<Province>(sql);
                return items.AsList();
            }
        }
    }
}
