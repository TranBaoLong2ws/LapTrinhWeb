using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020656.DataLayers.Interfaces;
using SV22T1020656.Models.Common;
using SV22T1020656.Models.Sales;
using System.Data;

namespace SV22T1020656
{
    /// <summary>
    /// Lớp truy xuất dữ liệu cho Orders và OrderDetails
    /// </summary>
    public class OrderRepository : IOrderRepository
    {
        /// <summary>
        /// Chuỗi kết nối database
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo repository
        /// </summary>
        public OrderRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Mở kết nối CSDL
        /// </summary>
        private IDbConnection OpenConnection()
        {
            return new SqlConnection(_connectionString);
        }

        #region Orders

        /// <summary>
        /// Tìm kiếm danh sách đơn hàng
        /// </summary>
        public async Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input)
        {
            using var connection = OpenConnection();

            var result = new PagedResult<OrderViewInfo>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            var parameters = new DynamicParameters();
            string condition = "WHERE 1=1";

            if (input.CustomerID > 0)
            {
                condition += " AND o.CustomerID = @CustomerID";
                parameters.Add("@CustomerID", input.CustomerID);
            }

            // Chấp nhận tất cả giá trị khác 0 (bao gồm cả -1, -2)
            if (input.Status != 0)
            {
                condition += " AND o.Status = @Status";
                parameters.Add("@Status", input.Status);
            }

            if (input.DateFrom.HasValue)
            {
                condition += " AND o.OrderTime>=@FromTime";
                parameters.Add("@FromTime", input.DateFrom);
            }

            if (input.DateTo.HasValue)
            {
                condition += " AND o.OrderTime<=@ToTime";
                parameters.Add("@ToTime", input.DateTo);
            }

            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                condition += @" AND (
                        c.CustomerName LIKE @SearchValue
                        OR c.Phone LIKE @SearchValue
                    )";

                parameters.Add("@SearchValue", $"%{input.SearchValue}%");
            }

            string countSQL = $@"
                SELECT COUNT(*)
                FROM Orders o
                LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                {condition}";

            result.RowCount = await connection.ExecuteScalarAsync<int>(countSQL, parameters);

            string dataSQL = $@"
                SELECT 
                    o.*,
                    c.CustomerName,
                    e.FullName EmployeeName,
                    s.ShipperName
                FROM Orders o
                LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                {condition}
                ORDER BY o.OrderTime DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add("@Offset", input.Offset);
            parameters.Add("@PageSize", input.PageSize);

            var data = await connection.QueryAsync<OrderViewInfo>(dataSQL, parameters);

            result.DataItems = data.ToList();

            return result;
        }

        /// <summary>
        /// Lấy thông tin 1 đơn hàng
        /// </summary>
        public async Task<OrderViewInfo?> GetAsync(int orderID)
        {
            using var connection = OpenConnection();

            string sql = @"
                SELECT 
                    o.*,
                    c.CustomerName,
                    e.FullName EmployeeName,
                    s.ShipperName
                FROM Orders o
                LEFT JOIN Customers c ON o.CustomerID=c.CustomerID
                LEFT JOIN Employees e ON o.EmployeeID=e.EmployeeID
                LEFT JOIN Shippers s ON o.ShipperID=s.ShipperID
                WHERE o.OrderID=@orderID";

            return await connection.QueryFirstOrDefaultAsync<OrderViewInfo>(sql, new { orderID });
        }

        /// <summary>
        /// Thêm đơn hàng
        /// </summary>
        public async Task<int> AddAsync(Order data)
        {
            using var connection = OpenConnection();

            string sql = @"
                INSERT INTO Orders
                (
                    CustomerID,
                    OrderTime,
                    DeliveryProvince,
                    DeliveryAddress,
                    EmployeeID,
                    AcceptTime,
                    ShipperID,
                    ShippedTime,
                    FinishedTime,
                    Status
                )
                VALUES
                (
                    @CustomerID,
                    @OrderTime,
                    @DeliveryProvince,
                    @DeliveryAddress,
                    @EmployeeID,
                    @AcceptTime,
                    @ShipperID,
                    @ShippedTime,
                    @FinishedTime,
                    @Status
                );
                SELECT SCOPE_IDENTITY();";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật đơn hàng
        /// </summary>
        public async Task<bool> UpdateAsync(Order data)
        {
            using var connection = OpenConnection();

            string sql = @"
                UPDATE Orders SET
                    CustomerID=@CustomerID,
                    DeliveryProvince=@DeliveryProvince,
                    DeliveryAddress=@DeliveryAddress,
                    EmployeeID=@EmployeeID,
                    AcceptTime=@AcceptTime,
                    ShipperID=@ShipperID,
                    ShippedTime=@ShippedTime,
                    FinishedTime=@FinishedTime,
                    Status=@Status
                WHERE OrderID=@OrderID";

            return await connection.ExecuteAsync(sql, data) > 0;
        }

        /// <summary>
        /// Xóa đơn hàng
        /// </summary>
        public async Task<bool> DeleteAsync(int orderID)
        {
            using var connection = OpenConnection();

            string sql = @"DELETE FROM Orders WHERE OrderID=@orderID";

            return await connection.ExecuteAsync(sql, new { orderID }) > 0;
        }

        #endregion


        #region OrderDetails

        /// <summary>
        /// Lấy danh sách mặt hàng trong đơn hàng
        /// </summary>
        public async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            using var connection = OpenConnection();

            string sql = @"
                SELECT 
                    d.*,
                    p.ProductName,
                    p.Photo
                FROM OrderDetails d
                JOIN Products p ON d.ProductID=p.ProductID
                WHERE d.OrderID=@orderID";

            var data = await connection.QueryAsync<OrderDetailViewInfo>(sql, new { orderID });

            return data.ToList();
        }

        /// <summary>
        /// Lấy 1 mặt hàng trong đơn hàng
        /// </summary>
        public async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            using var connection = OpenConnection();

            string sql = @"
                SELECT 
                    d.*,
                    p.ProductName,
                    p.Photo
                FROM OrderDetails d
                JOIN Products p ON d.ProductID=p.ProductID
                WHERE d.OrderID=@orderID
                AND d.ProductID=@productID";

            return await connection.QueryFirstOrDefaultAsync<OrderDetailViewInfo>(
                sql,
                new { orderID, productID }
            );
        }

        /// <summary>
        /// Thêm sản phẩm vào đơn hàng
        /// </summary>
        public async Task<bool> AddDetailAsync(OrderDetail data)
        {
            using var connection = OpenConnection();

            string sql = @"
                INSERT INTO OrderDetails
                (
                    OrderID,
                    ProductID,
                    Quantity,
                    SalePrice
                )
                VALUES
                (
                    @OrderID,
                    @ProductID,
                    @Quantity,
                    @SalePrice
                )";

            return await connection.ExecuteAsync(sql, data) > 0;
        }

        /// <summary>
        /// Cập nhật chi tiết đơn hàng
        /// </summary>
        public async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            using var connection = OpenConnection();

            string sql = @"
                UPDATE OrderDetails SET
                    Quantity=@Quantity,
                    SalePrice=@SalePrice
                WHERE OrderID=@OrderID
                AND ProductID=@ProductID";

            return await connection.ExecuteAsync(sql, data) > 0;
        }

        /// <summary>
        /// Xóa sản phẩm khỏi đơn hàng
        /// </summary>
        public async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            using var connection = OpenConnection();

            string sql = @"
                DELETE FROM OrderDetails
                WHERE OrderID=@orderID
                AND ProductID=@productID";

            return await connection.ExecuteAsync(sql, new { orderID, productID }) > 0;
        }

        #endregion
    }
}