using SV22T1020494.DataLayers.Interfaces;
using SV22T1020494.Models.Common;
using SV22T1020494.Models.Sales;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace SV22T1020494.DataLayers.SQLServer
{
    public class OrderRepository : IOrderRepository
    {
        private readonly string _connectionString;
        public OrderRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> AddAsync(Order data)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = @"INSERT INTO Orders (CustomerID, OrderTime, DeliveryProvince, DeliveryAddress, EmployeeID, AcceptTime, ShipperID, ShippedTime, FinishedTime, Status)
OUTPUT INSERTED.OrderID
VALUES (@CustomerID, @OrderTime, @DeliveryProvince, @DeliveryAddress, @EmployeeID, @AcceptTime, @ShipperID, @ShippedTime, @FinishedTime, @Status)";

            cmd.Parameters.AddWithValue("@CustomerID", (object?)data.CustomerID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@OrderTime", data.OrderTime);
            cmd.Parameters.AddWithValue("@DeliveryProvince", (object?)data.DeliveryProvince ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DeliveryAddress", (object?)data.DeliveryAddress ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EmployeeID", (object?)data.EmployeeID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AcceptTime", (object?)data.AcceptTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ShipperID", (object?)data.ShipperID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ShippedTime", (object?)data.ShippedTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FinishedTime", (object?)data.FinishedTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", (int)data.Status);

            await cn.OpenAsync();
            var idObj = await cmd.ExecuteScalarAsync();
            return idObj == null ? 0 : Convert.ToInt32(idObj);
        }

        public async Task<bool> AddDetailAsync(OrderDetail data)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = @"INSERT INTO OrderDetails (OrderID, ProductID, Quantity, SalePrice)
VALUES (@OrderID, @ProductID, @Quantity, @SalePrice)";
            cmd.Parameters.AddWithValue("@OrderID", data.OrderID);
            cmd.Parameters.AddWithValue("@ProductID", data.ProductID);
            cmd.Parameters.AddWithValue("@Quantity", data.Quantity);
            cmd.Parameters.AddWithValue("@SalePrice", data.SalePrice);

            await cn.OpenAsync();
            var affected = await cmd.ExecuteNonQueryAsync();
            return affected > 0;
        }

        public async Task<bool> DeleteAsync(int orderID)
        {
            using var cn = new SqlConnection(_connectionString);
            await cn.OpenAsync();
            using var tx = await cn.BeginTransactionAsync();
            try
            {
                // Delete order details first
                var cmdDelDetails = cn.CreateCommand();
                cmdDelDetails.Transaction = (SqlTransaction)tx;
                cmdDelDetails.CommandText = @"DELETE FROM OrderDetails WHERE OrderID = @orderID";
                cmdDelDetails.Parameters.AddWithValue("@orderID", orderID);
                await cmdDelDetails.ExecuteNonQueryAsync();

                // Delete order
                var cmdDelOrder = cn.CreateCommand();
                cmdDelOrder.Transaction = (SqlTransaction)tx;
                cmdDelOrder.CommandText = @"DELETE FROM Orders WHERE OrderID = @orderID";
                cmdDelOrder.Parameters.AddWithValue("@orderID", orderID);
                var affected = await cmdDelOrder.ExecuteNonQueryAsync();

                await tx.CommitAsync();
                return affected > 0;
            }
            catch
            {
                try { await tx.RollbackAsync(); } catch { }
                throw;
            }
        }

        public async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            throw new NotImplementedException();
        }

        public async Task<OrderViewInfo?> GetAsync(int orderID)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            // Select with joins to get customer/employee and total amount from OrderDetails
            cmd.CommandText = @"SELECT o.OrderID, o.CustomerID, o.OrderTime, o.DeliveryProvince, o.DeliveryAddress, o.EmployeeID, o.AcceptTime, o.ShipperID, o.ShippedTime, o.FinishedTime, o.Status,
                                           c.CustomerName, c.ContactName AS CustomerContactName, c.Email AS CustomerEmail, c.Phone AS CustomerPhone, c.Address AS CustomerAddress,
                                           e.FullName AS EmployeeName,
                                           s.ShipperName, s.Phone AS ShipperPhone,
                                           ISNULL(odt.TotalAmount,0) AS TotalAmount
                                    FROM Orders o
                                    LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                                    LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                                    LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                                    LEFT JOIN (
                                        SELECT OrderID, SUM(Quantity * SalePrice) AS TotalAmount
                                        FROM OrderDetails
                                        GROUP BY OrderID
                                    ) odt ON odt.OrderID = o.OrderID
                                    WHERE o.OrderID = @id";
            cmd.Parameters.AddWithValue("@id", orderID);
            await cn.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                var ov = new OrderViewInfo
                {
                    OrderID = r.GetInt32(r.GetOrdinal("OrderID")),
                    CustomerID = r.IsDBNull(r.GetOrdinal("CustomerID")) ? null : (int?)r.GetInt32(r.GetOrdinal("CustomerID")),
                    OrderTime = r.GetDateTime(r.GetOrdinal("OrderTime")),
                    DeliveryProvince = r.IsDBNull(r.GetOrdinal("DeliveryProvince")) ? null : r.GetString(r.GetOrdinal("DeliveryProvince")),
                    DeliveryAddress = r.IsDBNull(r.GetOrdinal("DeliveryAddress")) ? null : r.GetString(r.GetOrdinal("DeliveryAddress")),
                    EmployeeID = r.IsDBNull(r.GetOrdinal("EmployeeID")) ? null : (int?)r.GetInt32(r.GetOrdinal("EmployeeID")),
                    AcceptTime = r.IsDBNull(r.GetOrdinal("AcceptTime")) ? null : (DateTime?)r.GetDateTime(r.GetOrdinal("AcceptTime")),
                    ShipperID = r.IsDBNull(r.GetOrdinal("ShipperID")) ? null : (int?)r.GetInt32(r.GetOrdinal("ShipperID")),
                    ShippedTime = r.IsDBNull(r.GetOrdinal("ShippedTime")) ? null : (DateTime?)r.GetDateTime(r.GetOrdinal("ShippedTime")),
                    FinishedTime = r.IsDBNull(r.GetOrdinal("FinishedTime")) ? null : (DateTime?)r.GetDateTime(r.GetOrdinal("FinishedTime")),
                    Status = (OrderStatusEnum)r.GetInt32(r.GetOrdinal("Status")),

                    EmployeeName = r.IsDBNull(r.GetOrdinal("EmployeeName")) ? string.Empty : r.GetString(r.GetOrdinal("EmployeeName")),
                    CustomerName = r.IsDBNull(r.GetOrdinal("CustomerName")) ? string.Empty : r.GetString(r.GetOrdinal("CustomerName")),
                    CustomerContactName = r.IsDBNull(r.GetOrdinal("CustomerContactName")) ? string.Empty : r.GetString(r.GetOrdinal("CustomerContactName")),
                    CustomerEmail = r.IsDBNull(r.GetOrdinal("CustomerEmail")) ? string.Empty : r.GetString(r.GetOrdinal("CustomerEmail")),
                    CustomerPhone = r.IsDBNull(r.GetOrdinal("CustomerPhone")) ? string.Empty : r.GetString(r.GetOrdinal("CustomerPhone")),
                    CustomerAddress = r.IsDBNull(r.GetOrdinal("CustomerAddress")) ? string.Empty : r.GetString(r.GetOrdinal("CustomerAddress")),
                    ShipperName = r.IsDBNull(r.GetOrdinal("ShipperName")) ? string.Empty : r.GetString(r.GetOrdinal("ShipperName")),
                    ShipperPhone = r.IsDBNull(r.GetOrdinal("ShipperPhone")) ? string.Empty : r.GetString(r.GetOrdinal("ShipperPhone")),
                    TotalAmount = r.IsDBNull(r.GetOrdinal("TotalAmount")) ? 0 : r.GetDecimal(r.GetOrdinal("TotalAmount"))
                };

                return ov;
            }
            return null;
        }

        public async Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input)
        {
            var result = new PagedResult<OrderViewInfo>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = 0,
                DataItems = new List<OrderViewInfo>()
            };

            using var cn = new SqlConnection(_connectionString);
            var cmdCount = cn.CreateCommand();

            var whereParts = new List<string>();
            if (input.Status != 0)
            {
                whereParts.Add("o.Status = @status");
                cmdCount.Parameters.AddWithValue("@status", (int)input.Status);
            }
            // filter by customer if provided
            if (input is SV22T1020494.Models.Sales.OrderSearchInput osi && osi.CustomerID.HasValue)
            {
                whereParts.Add("o.CustomerID = @customerId");
                cmdCount.Parameters.AddWithValue("@customerId", osi.CustomerID.Value);
            }
            if (input.DateFrom.HasValue)
            {
                whereParts.Add("o.OrderTime >= @dateFrom");
                cmdCount.Parameters.AddWithValue("@dateFrom", input.DateFrom.Value.Date);
            }
            if (input.DateTo.HasValue)
            {
                whereParts.Add("o.OrderTime <= @dateTo");
                cmdCount.Parameters.AddWithValue("@dateTo", input.DateTo.Value.Date.AddDays(1).AddTicks(-1));
            }
            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                // Search by customer name/contact/phone by joining Customers in the main select; here search uses customer fields via JOIN in final query
                whereParts.Add("(c.CustomerName LIKE @search OR c.ContactName LIKE @search OR c.Phone LIKE @search OR CAST(o.OrderID AS NVARCHAR(50)) LIKE @search)");
                cmdCount.Parameters.AddWithValue("@search", "%" + input.SearchValue + "%");
            }

            var where = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : string.Empty;

            cmdCount.CommandText = $@"SELECT COUNT(*)
FROM Orders o
LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
{where}";

            await cn.OpenAsync();
            var totalObj = await cmdCount.ExecuteScalarAsync();
            result.RowCount = totalObj == null ? 0 : Convert.ToInt32(totalObj);

            if (input.PageSize == 0)
            {
                var cmdAll = cn.CreateCommand();
                cmdAll.CommandText = $@"SELECT o.OrderID, o.CustomerID, o.OrderTime, o.DeliveryProvince, o.DeliveryAddress, o.EmployeeID, o.AcceptTime, o.ShipperID, o.ShippedTime, o.FinishedTime, o.Status,
                                               c.CustomerName, c.ContactName AS CustomerContactName, c.Email AS CustomerEmail, c.Phone AS CustomerPhone, c.Address AS CustomerAddress,
                                               e.FullName AS EmployeeName,
                                               s.ShipperName, s.Phone AS ShipperPhone,
                                               ISNULL(odt.TotalAmount,0) AS TotalAmount
                                        FROM Orders o
                                        LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                                        LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                                        LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                                        LEFT JOIN (
                                            SELECT OrderID, SUM(Quantity * SalePrice) AS TotalAmount
                                            FROM OrderDetails
                                            GROUP BY OrderID
                                        ) odt ON odt.OrderID = o.OrderID
                                        {where}
                                        ORDER BY o.OrderID ASC";
                foreach (SqlParameter p in cmdCount.Parameters)
                    cmdAll.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));

                using var r = await cmdAll.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    result.DataItems.Add(new OrderViewInfo
                    {
                        OrderID = r.GetInt32(r.GetOrdinal("OrderID")),
                        CustomerID = r.IsDBNull(r.GetOrdinal("CustomerID")) ? null : (int?)r.GetInt32(r.GetOrdinal("CustomerID")),
                        OrderTime = r.GetDateTime(r.GetOrdinal("OrderTime")),
                        DeliveryProvince = r.IsDBNull(r.GetOrdinal("DeliveryProvince")) ? null : r.GetString(r.GetOrdinal("DeliveryProvince")),
                        DeliveryAddress = r.IsDBNull(r.GetOrdinal("DeliveryAddress")) ? null : r.GetString(r.GetOrdinal("DeliveryAddress")),
                        EmployeeID = r.IsDBNull(r.GetOrdinal("EmployeeID")) ? null : (int?)r.GetInt32(r.GetOrdinal("EmployeeID")),
                        AcceptTime = r.IsDBNull(r.GetOrdinal("AcceptTime")) ? null : (DateTime?)r.GetDateTime(r.GetOrdinal("AcceptTime")),
                        ShipperID = r.IsDBNull(r.GetOrdinal("ShipperID")) ? null : (int?)r.GetInt32(r.GetOrdinal("ShipperID")),
                        ShippedTime = r.IsDBNull(r.GetOrdinal("ShippedTime")) ? null : (DateTime?)r.GetDateTime(r.GetOrdinal("ShippedTime")),
                        FinishedTime = r.IsDBNull(r.GetOrdinal("FinishedTime")) ? null : (DateTime?)r.GetDateTime(r.GetOrdinal("FinishedTime")),
                        Status = (OrderStatusEnum)r.GetInt32(r.GetOrdinal("Status")),

                        EmployeeName = r.IsDBNull(r.GetOrdinal("EmployeeName")) ? string.Empty : r.GetString(r.GetOrdinal("EmployeeName")),
                        CustomerName = r.IsDBNull(r.GetOrdinal("CustomerName")) ? string.Empty : r.GetString(r.GetOrdinal("CustomerName")),
                        CustomerContactName = r.IsDBNull(r.GetOrdinal("CustomerContactName")) ? string.Empty : r.GetString(r.GetOrdinal("CustomerContactName")),
                        CustomerEmail = r.IsDBNull(r.GetOrdinal("CustomerEmail")) ? string.Empty : r.GetString(r.GetOrdinal("CustomerEmail")),
                        CustomerPhone = r.IsDBNull(r.GetOrdinal("CustomerPhone")) ? string.Empty : r.GetString(r.GetOrdinal("CustomerPhone")),
                        CustomerAddress = r.IsDBNull(r.GetOrdinal("CustomerAddress")) ? string.Empty : r.GetString(r.GetOrdinal("CustomerAddress")),
                        ShipperName = r.IsDBNull(r.GetOrdinal("ShipperName")) ? string.Empty : r.GetString(r.GetOrdinal("ShipperName")),
                        ShipperPhone = r.IsDBNull(r.GetOrdinal("ShipperPhone")) ? string.Empty : r.GetString(r.GetOrdinal("ShipperPhone")),
                        TotalAmount = r.IsDBNull(r.GetOrdinal("TotalAmount")) ? 0 : r.GetDecimal(r.GetOrdinal("TotalAmount"))
                    });
                }
                return result;
            }

            if (result.RowCount == 0)
                return result;

            var offset = (input.Page - 1) * input.PageSize;
            var cmd = cn.CreateCommand();
            cmd.CommandText = $@"SELECT o.OrderID, o.CustomerID, o.OrderTime, o.DeliveryProvince, o.DeliveryAddress, o.EmployeeID, o.AcceptTime, o.ShipperID, o.ShippedTime, o.FinishedTime, o.Status,
                                           c.CustomerName, c.ContactName AS CustomerContactName, c.Email AS CustomerEmail, c.Phone AS CustomerPhone, c.Address AS CustomerAddress,
                                           e.FullName AS EmployeeName,
                                           s.ShipperName, s.Phone AS ShipperPhone,
                                           ISNULL(odt.TotalAmount,0) AS TotalAmount
                                    FROM Orders o
                                    LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                                    LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                                    LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                                    LEFT JOIN (
                                        SELECT OrderID, SUM(Quantity * SalePrice) AS TotalAmount
                                        FROM OrderDetails
                                        GROUP BY OrderID
                                    ) odt ON odt.OrderID = o.OrderID
                                    {where}
                                    ORDER BY o.OrderID ASC
                                    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
            foreach (SqlParameter p in cmdCount.Parameters)
                cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
            cmd.Parameters.AddWithValue("@offset", offset);
            cmd.Parameters.AddWithValue("@pageSize", input.PageSize);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.DataItems.Add(new OrderViewInfo
                {
                    OrderID = reader.GetInt32(reader.GetOrdinal("OrderID")),
                    CustomerID = reader.IsDBNull(reader.GetOrdinal("CustomerID")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("CustomerID")),
                    OrderTime = reader.GetDateTime(reader.GetOrdinal("OrderTime")),
                    DeliveryProvince = reader.IsDBNull(reader.GetOrdinal("DeliveryProvince")) ? null : reader.GetString(reader.GetOrdinal("DeliveryProvince")),
                    DeliveryAddress = reader.IsDBNull(reader.GetOrdinal("DeliveryAddress")) ? null : reader.GetString(reader.GetOrdinal("DeliveryAddress")),
                    EmployeeID = reader.IsDBNull(reader.GetOrdinal("EmployeeID")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                    AcceptTime = reader.IsDBNull(reader.GetOrdinal("AcceptTime")) ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("AcceptTime")),
                    ShipperID = reader.IsDBNull(reader.GetOrdinal("ShipperID")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("ShipperID")),
                    ShippedTime = reader.IsDBNull(reader.GetOrdinal("ShippedTime")) ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("ShippedTime")),
                    FinishedTime = reader.IsDBNull(reader.GetOrdinal("FinishedTime")) ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("FinishedTime")),
                    Status = (OrderStatusEnum)reader.GetInt32(reader.GetOrdinal("Status")),

                    EmployeeName = reader.IsDBNull(reader.GetOrdinal("EmployeeName")) ? string.Empty : reader.GetString(reader.GetOrdinal("EmployeeName")),
                    CustomerName = reader.IsDBNull(reader.GetOrdinal("CustomerName")) ? string.Empty : reader.GetString(reader.GetOrdinal("CustomerName")),
                    CustomerContactName = reader.IsDBNull(reader.GetOrdinal("CustomerContactName")) ? string.Empty : reader.GetString(reader.GetOrdinal("CustomerContactName")),
                    CustomerEmail = reader.IsDBNull(reader.GetOrdinal("CustomerEmail")) ? string.Empty : reader.GetString(reader.GetOrdinal("CustomerEmail")),
                    CustomerPhone = reader.IsDBNull(reader.GetOrdinal("CustomerPhone")) ? string.Empty : reader.GetString(reader.GetOrdinal("CustomerPhone")),
                    CustomerAddress = reader.IsDBNull(reader.GetOrdinal("CustomerAddress")) ? string.Empty : reader.GetString(reader.GetOrdinal("CustomerAddress")),
                    ShipperName = reader.IsDBNull(reader.GetOrdinal("ShipperName")) ? string.Empty : reader.GetString(reader.GetOrdinal("ShipperName")),
                    ShipperPhone = reader.IsDBNull(reader.GetOrdinal("ShipperPhone")) ? string.Empty : reader.GetString(reader.GetOrdinal("ShipperPhone")),
                    TotalAmount = reader.IsDBNull(reader.GetOrdinal("TotalAmount")) ? 0 : reader.GetDecimal(reader.GetOrdinal("TotalAmount"))
                });
            }

            return result;
        }

        public async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = @"SELECT OrderID, ProductID, Quantity, SalePrice
                                FROM OrderDetails
                                WHERE OrderID = @orderID AND ProductID = @productID";
            cmd.Parameters.AddWithValue("@orderID", orderID);
            cmd.Parameters.AddWithValue("@productID", productID);

            await cn.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                var detail = new OrderDetailViewInfo
                {
                    OrderID = r.GetInt32(r.GetOrdinal("OrderID")),
                    ProductID = r.GetInt32(r.GetOrdinal("ProductID")),
                    Quantity = r.GetInt32(r.GetOrdinal("Quantity")),
                    SalePrice = r.GetDecimal(r.GetOrdinal("SalePrice"))
                };
                return detail;
            }
            return null;
        }

        public async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            var list = new List<OrderDetailViewInfo>();
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = @"SELECT OrderID, ProductID, Quantity, SalePrice
                                FROM OrderDetails
                                WHERE OrderID = @orderID
                                ORDER BY ProductID";
            cmd.Parameters.AddWithValue("@orderID", orderID);

            await cn.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new OrderDetailViewInfo
                {
                    OrderID = r.GetInt32(r.GetOrdinal("OrderID")),
                    ProductID = r.GetInt32(r.GetOrdinal("ProductID")),
                    Quantity = r.GetInt32(r.GetOrdinal("Quantity")),
                    SalePrice = r.GetDecimal(r.GetOrdinal("SalePrice"))
                });
            }

            return list;
        }

        public async Task<bool> UpdateAsync(Order data)
        {
            using var cn = new SqlConnection(_connectionString);
            var cmd = cn.CreateCommand();
            cmd.CommandText = @"UPDATE Orders SET
    CustomerID = @CustomerID,
    OrderTime = @OrderTime,
    DeliveryProvince = @DeliveryProvince,
    DeliveryAddress = @DeliveryAddress,
    EmployeeID = @EmployeeID,
    AcceptTime = @AcceptTime,
    ShipperID = @ShipperID,
    ShippedTime = @ShippedTime,
    FinishedTime = @FinishedTime,
    Status = @Status
WHERE OrderID = @OrderID";
            cmd.Parameters.AddWithValue("@CustomerID", (object?)data.CustomerID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@OrderTime", data.OrderTime);
            cmd.Parameters.AddWithValue("@DeliveryProvince", (object?)data.DeliveryProvince ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DeliveryAddress", (object?)data.DeliveryAddress ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EmployeeID", (object?)data.EmployeeID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AcceptTime", (object?)data.AcceptTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ShipperID", (object?)data.ShipperID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ShippedTime", (object?)data.ShippedTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FinishedTime", (object?)data.FinishedTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", (int)data.Status);
            cmd.Parameters.AddWithValue("@OrderID", data.OrderID);

            return await ExecuteNonQueryAsync(cn, cmd);
        }

        private static async Task<bool> ExecuteNonQueryAsync(SqlConnection cn, SqlCommand cmd)
        {
            // Ensure command is associated with the connection passed in
            if (cmd.Connection == null)
            {
                cmd.Connection = cn;
            }
            else if (!object.ReferenceEquals(cmd.Connection, cn))
            {
                // make sure to use the provided connection
                cmd.Connection = cn;
            }

            try
            {
                if (cn.State != System.Data.ConnectionState.Open)
                {
                    await cn.OpenAsync();
                }

                var affected = await cmd.ExecuteNonQueryAsync();
                return affected > 0;
            }
            finally
            {
                // Caller owns connection disposal (cn provided by caller with using), do not close here
            }
        }

        public Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            throw new NotImplementedException();
        }
    }
}
