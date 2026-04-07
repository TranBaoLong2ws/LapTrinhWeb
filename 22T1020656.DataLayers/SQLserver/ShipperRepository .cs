using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020656.DataLayers.Interfaces;
using SV22T1020656.Models.Common;
using SV22T1020656.Models.Partner;
using System.Data;

namespace SV22T1020656
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy xuất dữ liệu đối với bảng Shippers
    /// </summary>
    public class ShipperRepository : IGenericRepository<Shipper>
    {
        /// <summary>
        /// Chuỗi kết nối tới cơ sở dữ liệu
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo đối tượng ShipperRepository
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối tới cơ sở dữ liệu</param>
        public ShipperRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Mở kết nối tới cơ sở dữ liệu
        /// </summary>
        /// <returns>Đối tượng kết nối</returns>
        private IDbConnection OpenConnection()
        {
            return new SqlConnection(_connectionString);
        }

        /// <summary>
        /// Truy vấn danh sách người giao hàng theo điều kiện tìm kiếm và phân trang
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả truy vấn dưới dạng phân trang</returns>
        public async Task<PagedResult<Shipper>> ListAsync(PaginationSearchInput input)
        {
            using var connection = OpenConnection();

            var result = new PagedResult<Shipper>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            string condition = "";
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                condition = @"WHERE ShipperName LIKE @SearchValue
                              OR Phone LIKE @SearchValue";

                parameters.Add("@SearchValue", $"%{input.SearchValue}%");
            }

            string countSql = $@"
                    SELECT COUNT(*)
                    FROM Shippers
                    {condition}";

            result.RowCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            string dataSql = $@"
                    SELECT
                        ShipperID,
                        ShipperName,
                        Phone
                    FROM Shippers
                    {condition}
                    ORDER BY ShipperName
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add("@Offset", input.Offset);
            parameters.Add("@PageSize", input.PageSize);

            var data = await connection.QueryAsync<Shipper>(dataSql, parameters);
            result.DataItems = data.ToList();

            return result;
        }

        /// <summary>
        /// Lấy thông tin một người giao hàng theo mã
        /// </summary>
        /// <param name="id">Mã người giao hàng</param>
        /// <returns>Thông tin người giao hàng hoặc null nếu không tồn tại</returns>
        public async Task<Shipper?> GetAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT
                               ShipperID,
                               ShipperName,
                               Phone
                           FROM Shippers
                           WHERE ShipperID = @ShipperID";

            return await connection.QueryFirstOrDefaultAsync<Shipper>(sql, new
            {
                ShipperID = id
            });
        }

        /// <summary>
        /// Thêm mới người giao hàng vào cơ sở dữ liệu
        /// </summary>
        /// <param name="data">Thông tin người giao hàng cần thêm</param>
        /// <returns>Mã người giao hàng vừa được tạo</returns>
        public async Task<int> AddAsync(Shipper data)
        {
            using var connection = OpenConnection();

            string sql = @"
                INSERT INTO Shippers
                (
                    ShipperName,
                    Phone
                )
                VALUES
                (
                    @ShipperName,
                    @Phone
                );
                SELECT SCOPE_IDENTITY();";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin người giao hàng
        /// </summary>
        /// <param name="data">Dữ liệu cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Shipper data)
        {
            using var connection = OpenConnection();

            string sql = @"
                UPDATE Shippers
                SET
                    ShipperName = @ShipperName,
                    Phone = @Phone
                WHERE ShipperID = @ShipperID";

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa một người giao hàng khỏi cơ sở dữ liệu
        /// </summary>
        /// <param name="id">Mã người giao hàng cần xóa</param>
        /// <returns>true nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"DELETE FROM Shippers
                           WHERE ShipperID = @ShipperID";

            int rows = await connection.ExecuteAsync(sql, new
            {
                ShipperID = id
            });

            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra người giao hàng có đang được sử dụng trong bảng Orders hay không
        /// </summary>
        /// <param name="id">Mã người giao hàng</param>
        /// <returns>true nếu đang được sử dụng</returns>
        public async Task<bool> IsUsed(int id)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT COUNT(*)
                           FROM Orders
                           WHERE ShipperID = @ShipperID";

            int count = await connection.ExecuteScalarAsync<int>(sql, new
            {
                ShipperID = id
            });

            return count > 0;
        }
    }
}