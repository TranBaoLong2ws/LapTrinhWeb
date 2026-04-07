using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020656.DataLayers.Interfaces;
using SV22T1020656.Models.Common;
using SV22T1020656.Models.Partner;
using System.Data;

namespace SV22T1020656
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy xuất dữ liệu đối với bảng Customers
    /// </summary>
    public class CustomerRepository : ICustomerRepository
    {
        /// <summary>
        /// Chuỗi kết nối tới cơ sở dữ liệu
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo đối tượng CustomerRepository
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối tới CSDL</param>
        public CustomerRepository(string connectionString)
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
        /// Truy vấn danh sách khách hàng theo điều kiện tìm kiếm và phân trang
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả truy vấn dạng phân trang</returns>
        public async Task<PagedResult<Customer>> ListAsync(PaginationSearchInput input)
        {
            using var connection = OpenConnection();

            var result = new PagedResult<Customer>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            string condition = "";
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                condition = @"WHERE CustomerName LIKE @SearchValue
                              OR ContactName LIKE @SearchValue
                              OR Phone LIKE @SearchValue
                              OR Email LIKE @SearchValue";

                parameters.Add("@SearchValue", $"%{input.SearchValue}%");
            }

            string countSql = $@"
                    SELECT COUNT(*)
                    FROM Customers
                    {condition}";

            result.RowCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            string dataSql = $@"
                    SELECT
                        CustomerID,
                        CustomerName,
                        ContactName,
                        Province,
                        Address,
                        Phone,
                        Email,
                        IsLocked
                    FROM Customers
                    {condition}
                    ORDER BY CustomerName
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add("@Offset", input.Offset);
            parameters.Add("@PageSize", input.PageSize);

            var data = await connection.QueryAsync<Customer>(dataSql, parameters);
            result.DataItems = data.ToList();

            return result;
        }

        /// <summary>
        /// Lấy thông tin một khách hàng theo mã
        /// </summary>
        /// <param name="id">Mã khách hàng</param>
        /// <returns>Thông tin khách hàng hoặc null nếu không tồn tại</returns>
        public async Task<Customer?> GetAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT
                                CustomerID,
                                CustomerName,
                                ContactName,
                                Province,
                                Address,
                                Phone,
                                Email,
                                IsLocked
                           FROM Customers
                           WHERE CustomerID = @CustomerID";

            return await connection.QueryFirstOrDefaultAsync<Customer>(sql, new
            {
                CustomerID = id
            });
        }

        /// <summary>
        /// Thêm mới khách hàng vào cơ sở dữ liệu
        /// </summary>
        /// <param name="data">Thông tin khách hàng cần thêm</param>
        /// <returns>Mã khách hàng vừa được tạo</returns>
        public async Task<int> AddAsync(Customer data)
        {
            using var connection = OpenConnection();

            string sql = @"
                INSERT INTO Customers
                (
                    CustomerName,
                    ContactName,
                    Province,
                    Address,
                    Phone,
                    Email,
                    IsLocked
                )
                VALUES
                (
                    @CustomerName,
                    @ContactName,
                    @Province,
                    @Address,
                    @Phone,
                    @Email,
                    @IsLocked
                );
                SELECT SCOPE_IDENTITY();";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin khách hàng
        /// </summary>
        /// <param name="data">Dữ liệu cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Customer data)
        {
            using var connection = OpenConnection();

            string sql = @"
                UPDATE Customers
                SET
                    CustomerName = @CustomerName,
                    ContactName = @ContactName,
                    Province = @Province,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email,
                    IsLocked = @IsLocked
                WHERE CustomerID = @CustomerID";

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa khách hàng khỏi cơ sở dữ liệu
        /// </summary>
        /// <param name="id">Mã khách hàng cần xóa</param>
        /// <returns>true nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"DELETE FROM Customers
                           WHERE CustomerID = @CustomerID";

            int rows = await connection.ExecuteAsync(sql, new
            {
                CustomerID = id
            });

            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra khách hàng có đang được sử dụng trong bảng Orders hay không
        /// </summary>
        /// <param name="id">Mã khách hàng</param>
        /// <returns>true nếu đang được sử dụng</returns>
        public async Task<bool> IsUsed(int id)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT COUNT(*)
                           FROM Orders
                           WHERE CustomerID = @CustomerID";

            int count = await connection.ExecuteScalarAsync<int>(sql, new
            {
                CustomerID = id
            });

            return count > 0;
        }

        /// <summary>
        /// Kiểm tra email có hợp lệ hay không (không bị trùng)
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">
        /// Nếu id = 0: kiểm tra khi thêm mới khách hàng
        /// Nếu id ≠ 0: kiểm tra khi cập nhật khách hàng
        /// </param>
        /// <returns>true nếu email hợp lệ</returns>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = OpenConnection();

            string sql;

            if (id == 0)
            {
                sql = @"SELECT COUNT(*)
                        FROM Customers
                        WHERE Email = @Email";
                int count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email });
                return count == 0;
            }
            else
            {
                sql = @"SELECT COUNT(*)
                        FROM Customers
                        WHERE Email = @Email AND CustomerID <> @CustomerID";

                int count = await connection.ExecuteScalarAsync<int>(sql, new
                {
                    Email = email,
                    CustomerID = id
                });

                return count == 0;
            }
        }

        public async Task<bool> ChangePasswordAsync(int customerId, string newPassword)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // newPassword ở đây đã là chuỗi MD5 từ Controller truyền xuống
                var sql = @"UPDATE Customers SET Password = @pw WHERE CustomerID = @id";
                var rowsAffected = await connection.ExecuteAsync(sql, new { pw = newPassword, id = customerId });
                return rowsAffected > 0;
            }
        }
    }
}