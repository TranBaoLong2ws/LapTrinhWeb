using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020656.DataLayers.Interfaces;
using SV22T1020656.Models.Common;
using SV22T1020656.Models.Partner;
using System.Data;

namespace SV22T1020656
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy xuất dữ liệu đối với bảng Suppliers
    /// </summary>
    public class SupplierRepository : IGenericRepository<Supplier>
    {
        /// <summary>
        /// Chuỗi kết nối đến CSDL
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo đối tượng SupplierRepository
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến cơ sở dữ liệu</param>
        public SupplierRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Mở kết nối đến cơ sở dữ liệu
        /// </summary>
        /// <returns>Đối tượng SqlConnection</returns>
        private IDbConnection OpenConnection()
        {
            return new SqlConnection(_connectionString);
        }

        /// <summary>
        /// Truy vấn danh sách nhà cung cấp theo điều kiện tìm kiếm và phân trang
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả truy vấn dưới dạng phân trang</returns>
        public async Task<PagedResult<Supplier>> ListAsync(PaginationSearchInput input)
        {
            using var connection = OpenConnection();

            var result = new PagedResult<Supplier>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            string condition = "";
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                condition = @"WHERE SupplierName LIKE @SearchValue 
                              OR ContactName LIKE @SearchValue";
                parameters.Add("@SearchValue", $"%{input.SearchValue}%");
            }

            string countSql = $@"SELECT COUNT(*) 
                                 FROM Suppliers 
                                 {condition}";

            result.RowCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            string dataSql = $@"
                    SELECT *
                    FROM Suppliers
                    {condition}
                    ORDER BY SupplierName
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add("@Offset", input.Offset);
            parameters.Add("@PageSize", input.PageSize);

            var data = await connection.QueryAsync<Supplier>(dataSql, parameters);
            result.DataItems = data.ToList();

            return result;
        }

        /// <summary>
        /// Lấy thông tin một nhà cung cấp theo mã
        /// </summary>
        /// <param name="id">Mã nhà cung cấp</param>
        /// <returns>Thông tin nhà cung cấp (null nếu không tồn tại)</returns>
        public async Task<Supplier?> GetAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT *
                           FROM Suppliers
                           WHERE SupplierID = @SupplierID";

            return await connection.QueryFirstOrDefaultAsync<Supplier>(sql, new
            {
                SupplierID = id
            });
        }

        /// <summary>
        /// Bổ sung nhà cung cấp mới vào cơ sở dữ liệu
        /// </summary>
        /// <param name="data">Thông tin nhà cung cấp cần thêm</param>
        /// <returns>Mã nhà cung cấp vừa được tạo</returns>
        public async Task<int> AddAsync(Supplier data)
        {
            using var connection = OpenConnection();

            string sql = @"
                INSERT INTO Suppliers
                (
                    SupplierName,
                    ContactName,
                    Province,
                    Address,
                    Phone,
                    Email
                )
                VALUES
                (
                    @SupplierName,
                    @ContactName,
                    @Province,
                    @Address,
                    @Phone,
                    @Email
                );
                SELECT SCOPE_IDENTITY();";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin nhà cung cấp
        /// </summary>
        /// <param name="data">Dữ liệu cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Supplier data)
        {
            using var connection = OpenConnection();

            string sql = @"
                UPDATE Suppliers
                SET
                    SupplierName = @SupplierName,
                    ContactName = @ContactName,
                    Province = @Province,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email
                WHERE SupplierID = @SupplierID";

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa một nhà cung cấp khỏi cơ sở dữ liệu
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần xóa</param>
        /// <returns>true nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"DELETE FROM Suppliers 
                           WHERE SupplierID = @SupplierID";

            int rows = await connection.ExecuteAsync(sql, new
            {
                SupplierID = id
            });

            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra nhà cung cấp có đang được sử dụng trong bảng Products hay không
        /// </summary>
        /// <param name="id">Mã nhà cung cấp</param>
        /// <returns>true nếu đang được sử dụng</returns>
        public async Task<bool> IsUsed(int id)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT COUNT(*) 
                           FROM Products
                           WHERE SupplierID = @SupplierID";

            int count = await connection.ExecuteScalarAsync<int>(sql, new
            {
                SupplierID = id
            });

            return count > 0;
        }



        /// <summary>
        /// Kiểm tra email đã tồn tại trong hệ thống hay chưa
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <returns>true nếu đã tồn tại</returns>
        public async Task<bool> EmailExistsAsync(string email)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT COUNT(*)
                   FROM Suppliers
                   WHERE Email = @Email";

            int count = await connection.ExecuteScalarAsync<int>(sql, new
            {
                Email = email
            });

            return count > 0;
        }

    }
}