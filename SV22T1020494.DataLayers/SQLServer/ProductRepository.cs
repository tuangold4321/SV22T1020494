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
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;
        public ProductRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> AddAsync(Product data)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = "INSERT INTO Products(ProductName, ProductDescription, SupplierID, CategoryID, Unit, Price, Photo, IsSelling) VALUES(@name, @desc, @supplier, @category, @unit, @price, @photo, @isSelling); SELECT CAST(SCOPE_IDENTITY() AS int);";
            cmd.Parameters.AddWithValue("@name", data.ProductName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@desc", data.ProductDescription ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@supplier", data.SupplierID.HasValue ? (object)data.SupplierID.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@category", data.CategoryID.HasValue ? (object)data.CategoryID.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@unit", data.Unit ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@price", data.Price);
            cmd.Parameters.AddWithValue("@photo", data.Photo ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@isSelling", data.IsSelling ? 1 : 0);

            await cn.OpenAsync();
            var id = await cmd.ExecuteScalarAsync();
            return id == null ? 0 : Convert.ToInt32(id);
        }

        public async Task<bool> DeleteAsync(int productID)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = "DELETE FROM Products WHERE ProductID = @id";
            cmd.Parameters.AddWithValue("@id", productID);
            await cn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<Product?> GetAsync(int productID)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT ProductID, ProductName, ProductDescription, SupplierID, CategoryID, Unit, Price, Photo, IsSelling FROM Products WHERE ProductID = @id";
            cmd.Parameters.AddWithValue("@id", productID);
            await cn.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                return new Product
                {
                    ProductID = r.GetInt32(r.GetOrdinal("ProductID")),
                    ProductName = r.IsDBNull(r.GetOrdinal("ProductName")) ? string.Empty : r.GetString(r.GetOrdinal("ProductName")),
                    ProductDescription = r.IsDBNull(r.GetOrdinal("ProductDescription")) ? null : r.GetString(r.GetOrdinal("ProductDescription")),
                    SupplierID = r.IsDBNull(r.GetOrdinal("SupplierID")) ? null : (int?)r.GetInt32(r.GetOrdinal("SupplierID")),
                    CategoryID = r.IsDBNull(r.GetOrdinal("CategoryID")) ? null : (int?)r.GetInt32(r.GetOrdinal("CategoryID")),
                    Unit = r.IsDBNull(r.GetOrdinal("Unit")) ? string.Empty : r.GetString(r.GetOrdinal("Unit")),
                    Price = r.IsDBNull(r.GetOrdinal("Price")) ? 0 : r.GetDecimal(r.GetOrdinal("Price")),
                    Photo = r.IsDBNull(r.GetOrdinal("Photo")) ? null : r.GetString(r.GetOrdinal("Photo")),
                    IsSelling = !r.IsDBNull(r.GetOrdinal("IsSelling")) && r.GetBoolean(r.GetOrdinal("IsSelling"))
                };
            }
            return null;
        }

        public async Task<bool> IsUsedAsync(int productID)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            // Check OrderDetails or other tables referencing product
            cmd.CommandText = "SELECT TOP 1 1 FROM OrderDetails WHERE ProductID = @id";
            cmd.Parameters.AddWithValue("@id", productID);
            await cn.OpenAsync();
            var obj = await cmd.ExecuteScalarAsync();
            return obj != null;
        }

        public async Task<PagedResult<Product>> ListAsync(ProductSearchInput input)
        {
            var result = new PagedResult<Product>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = 0,
                DataItems = new List<Product>()
            };

            using var cn = new SqlConnection(_connectionString);
            var cmdCount = cn.CreateCommand();

            var whereParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                whereParts.Add("(ProductName LIKE @search OR ProductDescription LIKE @search)");
                cmdCount.Parameters.AddWithValue("@search", "%" + input.SearchValue + "%");
            }
            if (input.CategoryID > 0)
            {
                whereParts.Add("CategoryID = @category");
                cmdCount.Parameters.AddWithValue("@category", input.CategoryID);
            }
            if (input.SupplierID > 0)
            {
                whereParts.Add("SupplierID = @supplier");
                cmdCount.Parameters.AddWithValue("@supplier", input.SupplierID);
            }
            if (input.MinPrice > 0)
            {
                whereParts.Add("Price >= @minPrice");
                cmdCount.Parameters.AddWithValue("@minPrice", input.MinPrice);
            }
            if (input.MaxPrice > 0)
            {
                whereParts.Add("Price <= @maxPrice");
                cmdCount.Parameters.AddWithValue("@maxPrice", input.MaxPrice);
            }

            var where = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : string.Empty;
            cmdCount.CommandText = $"SELECT COUNT(*) FROM Products {where}";

            await cn.OpenAsync();
            var totalObj = await cmdCount.ExecuteScalarAsync();
            result.RowCount = totalObj == null ? 0 : Convert.ToInt32(totalObj);

            if (input.PageSize == 0)
            {
                var cmdAll = cn.CreateCommand();
                cmdAll.CommandText = $"SELECT ProductID, ProductName, ProductDescription, SupplierID, CategoryID, Unit, Price, Photo, IsSelling FROM Products {where} ORDER BY ProductID";
                foreach (SqlParameter p in cmdCount.Parameters)
                    cmdAll.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));

                using var r = await cmdAll.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    result.DataItems.Add(new Product
                    {
                        ProductID = r.GetInt32(r.GetOrdinal("ProductID")),
                        ProductName = r.IsDBNull(r.GetOrdinal("ProductName")) ? string.Empty : r.GetString(r.GetOrdinal("ProductName")),
                        ProductDescription = r.IsDBNull(r.GetOrdinal("ProductDescription")) ? null : r.GetString(r.GetOrdinal("ProductDescription")),
                        SupplierID = r.IsDBNull(r.GetOrdinal("SupplierID")) ? null : (int?)r.GetInt32(r.GetOrdinal("SupplierID")),
                        CategoryID = r.IsDBNull(r.GetOrdinal("CategoryID")) ? null : (int?)r.GetInt32(r.GetOrdinal("CategoryID")),
                        Unit = r.IsDBNull(r.GetOrdinal("Unit")) ? string.Empty : r.GetString(r.GetOrdinal("Unit")),
                        Price = r.IsDBNull(r.GetOrdinal("Price")) ? 0 : r.GetDecimal(r.GetOrdinal("Price")),
                        Photo = r.IsDBNull(r.GetOrdinal("Photo")) ? null : r.GetString(r.GetOrdinal("Photo")),
                        IsSelling = !r.IsDBNull(r.GetOrdinal("IsSelling")) && r.GetBoolean(r.GetOrdinal("IsSelling"))
                    });
                }
                return result;
            }

            if (result.RowCount == 0)
                return result;

            var offset = (input.Page - 1) * input.PageSize;
            var cmd = cn.CreateCommand();
            cmd.CommandText = $@"SELECT ProductID, ProductName, ProductDescription, SupplierID, CategoryID, Unit, Price, Photo, IsSelling
FROM Products
{where}
ORDER BY ProductID
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
            foreach (SqlParameter p in cmdCount.Parameters)
                cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
            cmd.Parameters.AddWithValue("@offset", offset);
            cmd.Parameters.AddWithValue("@pageSize", input.PageSize);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.DataItems.Add(new Product
                {
                    ProductID = reader.GetInt32(reader.GetOrdinal("ProductID")),
                    ProductName = reader.IsDBNull(reader.GetOrdinal("ProductName")) ? string.Empty : reader.GetString(reader.GetOrdinal("ProductName")),
                    ProductDescription = reader.IsDBNull(reader.GetOrdinal("ProductDescription")) ? null : reader.GetString(reader.GetOrdinal("ProductDescription")),
                    SupplierID = reader.IsDBNull(reader.GetOrdinal("SupplierID")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("SupplierID")),
                    CategoryID = reader.IsDBNull(reader.GetOrdinal("CategoryID")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("CategoryID")),
                    Unit = reader.IsDBNull(reader.GetOrdinal("Unit")) ? string.Empty : reader.GetString(reader.GetOrdinal("Unit")),
                    Price = reader.IsDBNull(reader.GetOrdinal("Price")) ? 0 : reader.GetDecimal(reader.GetOrdinal("Price")),
                    Photo = reader.IsDBNull(reader.GetOrdinal("Photo")) ? null : reader.GetString(reader.GetOrdinal("Photo")),
                    IsSelling = !reader.IsDBNull(reader.GetOrdinal("IsSelling")) && reader.GetBoolean(reader.GetOrdinal("IsSelling"))
                });
            }

            return result;
        }

        public async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT AttributeID, ProductID, AttributeName, AttributeValue, DisplayOrder FROM ProductAttributes WHERE AttributeID = @id";
            cmd.Parameters.AddWithValue("@id", attributeID);
            await cn.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                return new ProductAttribute
                {
                    AttributeID = r.GetInt64(r.GetOrdinal("AttributeID")),
                    ProductID = r.GetInt32(r.GetOrdinal("ProductID")),
                    AttributeName = r.IsDBNull(r.GetOrdinal("AttributeName")) ? string.Empty : r.GetString(r.GetOrdinal("AttributeName")),
                    AttributeValue = r.IsDBNull(r.GetOrdinal("AttributeValue")) ? string.Empty : r.GetString(r.GetOrdinal("AttributeValue")),
                    DisplayOrder = r.IsDBNull(r.GetOrdinal("DisplayOrder")) ? 0 : r.GetInt32(r.GetOrdinal("DisplayOrder"))
                };
            }
            return null;
        }

        public async Task<ProductPhoto?> GetPhotoAsync(long photoID)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT PhotoID, ProductID, Photo, Description, DisplayOrder, IsHidden FROM ProductPhotos WHERE PhotoID = @id";
            cmd.Parameters.AddWithValue("@id", photoID);
            await cn.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                return new ProductPhoto
                {
                    PhotoID = r.GetInt64(r.GetOrdinal("PhotoID")),
                    ProductID = r.GetInt32(r.GetOrdinal("ProductID")),
                    Photo = r.IsDBNull(r.GetOrdinal("Photo")) ? string.Empty : r.GetString(r.GetOrdinal("Photo")),
                    Description = r.IsDBNull(r.GetOrdinal("Description")) ? string.Empty : r.GetString(r.GetOrdinal("Description")),
                    DisplayOrder = r.IsDBNull(r.GetOrdinal("DisplayOrder")) ? 0 : r.GetInt32(r.GetOrdinal("DisplayOrder")),
                    IsHidden = !r.IsDBNull(r.GetOrdinal("IsHidden")) && r.GetBoolean(r.GetOrdinal("IsHidden"))
                };
            }
            return null;
        }

        public async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = "DELETE FROM ProductAttributes WHERE AttributeID = @id";
            cmd.Parameters.AddWithValue("@id", attributeID);
            await cn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeletePhotoAsync(long photoID)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = "DELETE FROM ProductPhotos WHERE PhotoID = @id";
            cmd.Parameters.AddWithValue("@id", photoID);
            await cn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = @"INSERT INTO ProductAttributes(ProductID, AttributeName, AttributeValue, DisplayOrder)
VALUES(@productId, @attributeName, @attributeValue, @displayOrder);
SELECT CAST(SCOPE_IDENTITY() AS bigint);";
            cmd.Parameters.AddWithValue("@productId", data.ProductID);
            cmd.Parameters.AddWithValue("@attributeName", data.AttributeName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@attributeValue", data.AttributeValue ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@displayOrder", data.DisplayOrder);

            await cn.OpenAsync();
            var id = await cmd.ExecuteScalarAsync();
            return id == null ? 0L : Convert.ToInt64(id);
        }

        public async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = @"INSERT INTO ProductPhotos(ProductID, Photo, Description, DisplayOrder, IsHidden)
VALUES(@productId, @photo, @description, @displayOrder, @isHidden);
SELECT CAST(SCOPE_IDENTITY() AS bigint);";
            cmd.Parameters.AddWithValue("@productId", data.ProductID);
            cmd.Parameters.AddWithValue("@photo", (object?)data.Photo ?? DBNull.Value);
            // Description column does not allow NULL in the database, ensure we send empty string instead of DBNull
            cmd.Parameters.AddWithValue("@description", data.Description ?? string.Empty);
            cmd.Parameters.AddWithValue("@displayOrder", data.DisplayOrder);
            cmd.Parameters.AddWithValue("@isHidden", data.IsHidden ? 1 : 0);

            await cn.OpenAsync();
            var id = await cmd.ExecuteScalarAsync();
            return id == null ? 0L : Convert.ToInt64(id);
        }

        public async Task<List<ProductAttribute>> ListAttributesAsync(int productID)
        {
            var result = new List<ProductAttribute>();
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT AttributeID, ProductID, AttributeName, AttributeValue, DisplayOrder FROM ProductAttributes WHERE ProductID = @id ORDER BY DisplayOrder, AttributeID";
            cmd.Parameters.AddWithValue("@id", productID);
            await cn.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                result.Add(new ProductAttribute
                {
                    AttributeID = r.GetInt64(r.GetOrdinal("AttributeID")),
                    ProductID = r.GetInt32(r.GetOrdinal("ProductID")),
                    AttributeName = r.IsDBNull(r.GetOrdinal("AttributeName")) ? string.Empty : r.GetString(r.GetOrdinal("AttributeName")),
                    AttributeValue = r.IsDBNull(r.GetOrdinal("AttributeValue")) ? string.Empty : r.GetString(r.GetOrdinal("AttributeValue")),
                    DisplayOrder = r.IsDBNull(r.GetOrdinal("DisplayOrder")) ? 0 : r.GetInt32(r.GetOrdinal("DisplayOrder"))
                });
            }
            return result;
        }

        public async Task<List<ProductPhoto>> ListPhotoAsync(int productID)
        {
            var result = new List<ProductPhoto>();
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT PhotoID, ProductID, Photo, Description, DisplayOrder, IsHidden FROM ProductPhotos WHERE ProductID = @id ORDER BY DisplayOrder, PhotoID";
            cmd.Parameters.AddWithValue("@id", productID);
            await cn.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                result.Add(new ProductPhoto
                {
                    PhotoID = r.GetInt64(r.GetOrdinal("PhotoID")),
                    ProductID = r.GetInt32(r.GetOrdinal("ProductID")),
                    Photo = r.IsDBNull(r.GetOrdinal("Photo")) ? string.Empty : r.GetString(r.GetOrdinal("Photo")),
                    Description = r.IsDBNull(r.GetOrdinal("Description")) ? string.Empty : r.GetString(r.GetOrdinal("Description")),
                    DisplayOrder = r.IsDBNull(r.GetOrdinal("DisplayOrder")) ? 0 : r.GetInt32(r.GetOrdinal("DisplayOrder")),
                    IsHidden = !r.IsDBNull(r.GetOrdinal("IsHidden")) && r.GetBoolean(r.GetOrdinal("IsHidden"))
                });
            }
            return result;
        }

        public async Task<bool> UpdateAsync(Product data)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = @"UPDATE Products SET ProductName = @name, ProductDescription = @desc, SupplierID = @supplier, CategoryID = @category, Unit = @unit, Price = @price, Photo = @photo, IsSelling = @isSelling
WHERE ProductID = @id";
            cmd.Parameters.AddWithValue("@name", data.ProductName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@desc", data.ProductDescription ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@supplier", data.SupplierID.HasValue ? (object)data.SupplierID.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@category", data.CategoryID.HasValue ? (object)data.CategoryID.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@unit", data.Unit ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@price", data.Price);
            cmd.Parameters.AddWithValue("@photo", data.Photo ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@isSelling", data.IsSelling ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", data.ProductID);

            await cn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = @"UPDATE ProductAttributes SET AttributeName = @attributeName, AttributeValue = @attributeValue, DisplayOrder = @displayOrder
WHERE AttributeID = @attributeId";
            cmd.Parameters.AddWithValue("@attributeName", data.AttributeName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@attributeValue", data.AttributeValue ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@displayOrder", data.DisplayOrder);
            cmd.Parameters.AddWithValue("@attributeId", data.AttributeID);

            await cn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = @"UPDATE ProductPhotos SET Photo = @photo, Description = @description, DisplayOrder = @displayOrder, IsHidden = @isHidden
WHERE PhotoID = @photoId";
            cmd.Parameters.AddWithValue("@photo", (object?)data.Photo ?? DBNull.Value);
            // Description column does not allow NULL in the database, ensure we send empty string instead of DBNull
            cmd.Parameters.AddWithValue("@description", data.Description ?? string.Empty);
            cmd.Parameters.AddWithValue("@displayOrder", data.DisplayOrder);
            cmd.Parameters.AddWithValue("@isHidden", data.IsHidden ? 1 : 0);
            cmd.Parameters.AddWithValue("@photoId", data.PhotoID);

            await cn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}
