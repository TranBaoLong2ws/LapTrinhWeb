using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020656.DataLayers.Interfaces;
using SV22T1020656.Models.Common;
using SV22T1020656.Models.HR;
using System.Data;

namespace SV22T1020656
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy xuất dữ liệu đối với bảng Employees
    /// </summary>
    public class EmployeeRepository : IEmployeeRepository
    {
        /// <summary>
        /// Chuỗi kết nối tới cơ sở dữ liệu
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo đối tượng EmployeeRepository
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối tới cơ sở dữ liệu</param>
        public EmployeeRepository(string connectionString)
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
        /// Truy vấn danh sách nhân viên theo điều kiện tìm kiếm và phân trang
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả truy vấn dạng phân trang</returns>
        public async Task<PagedResult<Employee>> ListAsync(PaginationSearchInput input)
        {
            using var connection = OpenConnection();

            var result = new PagedResult<Employee>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            string condition = "";
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                condition = @"WHERE FullName LIKE @SearchValue
                              OR Phone LIKE @SearchValue
                              OR Email LIKE @SearchValue";

                parameters.Add("@SearchValue", $"%{input.SearchValue}%");
            }

            string countSql = $@"
                SELECT COUNT(*)
                FROM Employees
                {condition}";

            result.RowCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            string dataSql = $@"
                SELECT 
                    EmployeeID,
                    FullName,
                    BirthDate,
                    Address,
                    Phone,
                    Email,
                    Photo,
                    IsWorking
                FROM Employees
                {condition}
                ORDER BY FullName
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add("@Offset", input.Offset);
            parameters.Add("@PageSize", input.PageSize);

            var data = await connection.QueryAsync<Employee>(dataSql, parameters);
            result.DataItems = data.ToList();

            return result;
        }

        /// <summary>
        /// Lấy thông tin một nhân viên theo mã
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <returns>Thông tin nhân viên hoặc null nếu không tồn tại</returns>
        public async Task<Employee?> GetAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT 
                               EmployeeID,
                               FullName,
                               BirthDate,
                               Address,
                               Phone,
                               Email,
                               Photo,
                               IsWorking
                           FROM Employees
                           WHERE EmployeeID = @EmployeeID";

            return await connection.QueryFirstOrDefaultAsync<Employee>(sql, new
            {
                EmployeeID = id
            });
        }

        /// <summary>
        /// Thêm mới nhân viên vào cơ sở dữ liệu
        /// </summary>
        /// <param name="data">Thông tin nhân viên cần thêm</param>
        /// <returns>Mã nhân viên vừa được tạo</returns>
        public async Task<int> AddAsync(Employee data)
        {
            using var connection = OpenConnection();

            string sql = @"
                INSERT INTO Employees
                (
                    FullName,
                    BirthDate,
                    Address,
                    Phone,
                    Email,
                    Photo,
                    IsWorking
                )
                VALUES
                (
                    @FullName,
                    @BirthDate,
                    @Address,
                    @Phone,
                    @Email,
                    @Photo,
                    @IsWorking
                );
                SELECT SCOPE_IDENTITY();";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin nhân viên
        /// </summary>
        /// <param name="data">Dữ liệu cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Employee data)
        {
            using var connection = OpenConnection();

            string sql = @"
                UPDATE Employees
                SET
                    FullName = @FullName,
                    BirthDate = @BirthDate,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email,
                    Photo = @Photo,
                    IsWorking = @IsWorking
                WHERE EmployeeID = @EmployeeID";

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa một nhân viên khỏi cơ sở dữ liệu
        /// </summary>
        /// <param name="id">Mã nhân viên cần xóa</param>
        /// <returns>true nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"DELETE FROM Employees
                           WHERE EmployeeID = @EmployeeID";

            int rows = await connection.ExecuteAsync(sql, new
            {
                EmployeeID = id
            });

            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra nhân viên có đang được sử dụng trong bảng Orders hay không
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <returns>true nếu đang được sử dụng</returns>
        public async Task<bool> IsUsed(int id)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT COUNT(*)
                           FROM Orders
                           WHERE EmployeeID = @EmployeeID";

            int count = await connection.ExecuteScalarAsync<int>(sql, new
            {
                EmployeeID = id
            });

            return count > 0;
        }

        /// <summary>
        /// Kiểm tra email của nhân viên có hợp lệ hay không (không bị trùng)
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">
        /// Nếu id = 0: kiểm tra khi thêm mới
        /// Nếu id ≠ 0: kiểm tra khi cập nhật
        /// </param>
        /// <returns>true nếu email hợp lệ</returns>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = OpenConnection();

            string sql;

            if (id == 0)
            {
                sql = @"SELECT COUNT(*)
                        FROM Employees
                        WHERE Email = @Email";
                int count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email });
                return count == 0;
            }
            else
            {
                sql = @"SELECT COUNT(*)
                        FROM Employees
                        WHERE Email = @Email
                        AND EmployeeID <> @EmployeeID";

                int count = await connection.ExecuteScalarAsync<int>(sql, new
                {
                    Email = email,
                    EmployeeID = id
                });

                return count == 0;
            }
        }

        public async Task<bool> ChangePasswordAsync(int employeeID, string newPassword)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Chú ý: Đảm bảo tên cột trong DB là 'Password' 
                // Nếu DB của bạn dùng tên khác (ví dụ: PasswordHash), hãy sửa lại ở đây.
                var sql = @"UPDATE Employees 
                            SET [Password] = @Password 
                            WHERE EmployeeID = @EmployeeID";

                // Thực thi câu lệnh SQL với tham số tường minh
                var rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    EmployeeID = employeeID,
                    Password = newPassword // Đây là chuỗi đã được mã hóa MD5 từ Service truyền xuống
                });

                return rowsAffected > 0;
            }
        }
    }
}