using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020656.DataLayers.Interfaces;
using SV22T1020656.Models.Common;
using SV22T1020656.Models.Catalog;
using System.Data;

namespace SV22T1020656
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy xuất dữ liệu đối với bảng Categories
    /// </summary>
    public class CategoryRepository : IGenericRepository<Category>
    {
        /// <summary>
        /// Chuỗi kết nối tới cơ sở dữ liệu
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo đối tượng CategoryRepository
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối tới cơ sở dữ liệu</param>
        public CategoryRepository(string connectionString)
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
        /// Truy vấn danh sách loại hàng theo điều kiện tìm kiếm và phân trang
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả truy vấn dạng phân trang</returns>
        public async Task<PagedResult<Category>> ListAsync(PaginationSearchInput input)
        {

            using var connection = OpenConnection();
            if (input.PageSize <= 0) input.PageSize = 20;

            var result = new PagedResult<Category>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            string condition = "";
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                condition = @"WHERE CategoryName LIKE @SearchValue";

                parameters.Add("@SearchValue", $"%{input.SearchValue}%");
            }

            string countSql = $@"
                SELECT COUNT(*)
                FROM Categories
                {condition}";

            result.RowCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            string dataSql = $@"
                SELECT 
                    CategoryID,
                    CategoryName,
                    Description
                FROM Categories
                {condition}
                ORDER BY CategoryName
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add("@Offset", input.Offset);
            parameters.Add("@PageSize", input.PageSize);

            var data = await connection.QueryAsync<Category>(dataSql, parameters);
            result.DataItems = data.ToList();

            return result;
        }

        /// <summary>
        /// Lấy thông tin một loại hàng theo mã
        /// </summary>
        /// <param name="id">Mã loại hàng</param>
        /// <returns>Thông tin loại hàng hoặc null nếu không tồn tại</returns>
        public async Task<Category?> GetAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT
                               CategoryID,
                               CategoryName,
                               Description
                           FROM Categories
                           WHERE CategoryID = @CategoryID";

            return await connection.QueryFirstOrDefaultAsync<Category>(sql, new
            {
                CategoryID = id
            });
        }

        /// <summary>
        /// Thêm mới loại hàng vào cơ sở dữ liệu
        /// </summary>
        /// <param name="data">Thông tin loại hàng cần thêm</param>
        /// <returns>Mã loại hàng vừa được tạo</returns>
        public async Task<int> AddAsync(Category data)
        {
            using var connection = OpenConnection();

            string sql = @"
                INSERT INTO Categories
                (
                    CategoryName,
                    Description
                )
                VALUES
                (
                    @CategoryName,
                    @Description
                );
                SELECT SCOPE_IDENTITY();";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin loại hàng
        /// </summary>
        /// <param name="data">Dữ liệu cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Category data)
        {
            using var connection = OpenConnection();

            string sql = @"
                UPDATE Categories
                SET
                    CategoryName = @CategoryName,
                    Description = @Description
                WHERE CategoryID = @CategoryID";

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa một loại hàng khỏi cơ sở dữ liệu
        /// </summary>
        /// <param name="id">Mã loại hàng cần xóa</param>
        /// <returns>true nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"DELETE FROM Categories
                           WHERE CategoryID = @CategoryID";

            int rows = await connection.ExecuteAsync(sql, new
            {
                CategoryID = id
            });

            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra loại hàng có đang được sử dụng trong bảng Products hay không
        /// </summary>
        /// <param name="id">Mã loại hàng</param>
        /// <returns>true nếu đang được sử dụng</returns>
        public async Task<bool> IsUsed(int id)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT COUNT(*)
                           FROM Products
                           WHERE CategoryID = @CategoryID";

            int count = await connection.ExecuteScalarAsync<int>(sql, new
            {
                CategoryID = id
            });

            return count > 0;
        }
    }
}