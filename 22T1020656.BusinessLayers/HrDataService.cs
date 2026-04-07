using SV22T1020656.DataLayers.Interfaces;
using SV22T1020656.Models.Common;
using SV22T1020656.Models.HR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020656.BusinessLayers
{
    public static class HrDataService
    {
        /// <summary>
        /// Đối tượng truy xuất dữ liệu nhân viên
        /// </summary>
        private static readonly IEmployeeRepository employeeDB;


        /// <summary>
        /// Hàm khởi tạo tĩnh của lớp HrDataService
        /// Dùng để khởi tạo repository truy cập dữ liệu Employee
        /// </summary>
        static HrDataService()
        {
            employeeDB = new EmployeeRepository(Configuration.ConnectionString);
        }

        // các chức năng nghiệp vụ liên quan đến nhân viên

        /// <summary>
        /// Lấy danh sách nhân viên theo điều kiện tìm kiếm và phân trang
        /// </summary>
        /// <param name="page">Trang cần lấy dữ liệu</param>
        /// <param name="pageSize">Số dòng trên mỗi trang</param>
        /// <param name="searchValue">Giá trị tìm kiếm (tên, email...)</param>
        /// <returns>
        /// Kết quả danh sách nhân viên dạng phân trang
        /// </returns>
        public static async Task<PagedResult<Employee>> ListEmployeeAsync(PaginationSearchInput input)
        {
            return await employeeDB.ListAsync(input);
        }



        /// <summary>
        /// Lấy thông tin chi tiết của một nhân viên theo mã nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <returns>Thông tin nhân viên nếu tồn tại, ngược lại trả về null</returns>
        public static async Task<Employee> GetEmployeeAsync(int employeeID)
        {
            return await employeeDB.GetAsync(employeeID);
        }


        /// <summary>
        /// Bổ sung nhân viên mới
        /// </summary>
        /// <param name="data">Thông tin nhân viên cần thêm</param>
        /// <returns>Mã nhân viên vừa được tạo</returns>
        public static async Task<int> AddEmployeeAsync(Employee employee)
        {
            return await employeeDB.AddAsync(employee);
        }



        /// <summary>
        /// Cập nhật thông tin nhân viên
        /// </summary>
        /// <param name="data">Thông tin nhân viên cần cập nhật</param>
        /// <returns>True nếu cập nhật thành công, ngược lại False</returns>
        public static async Task<bool> UpdateEmployeeAsync(Employee employee)
        {
            return await employeeDB.UpdateAsync(employee);
        }

        /// <summary>
        /// Xóa nhân viên theo mã nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên cần xóa</param>
        /// <returns>True nếu xóa thành công</returns>
        public static async Task<bool> DeleteEmployeeAsync(int id)
        {
            if (await employeeDB.IsUsed(id))
                return false;
            return await employeeDB.DeleteAsync(id);
        }

        public static async Task<bool> isUseAsync(int id)
        {
            return await employeeDB.IsUsed(id);
        }




        /// <summary>
        /// Kiểm tra email của nhân viên có hợp lệ hay không
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">
        /// Nếu id = 0: kiểm tra email của nhân viên mới  
        /// Nếu id ≠ 0: kiểm tra email của nhân viên đang cập nhật
        /// </param>
        /// <returns>True nếu email hợp lệ (không trùng)</returns>
        public static async Task<bool> ValidateEmailAsync(String email, int id)
        {
            return await employeeDB.ValidateEmailAsync(email,id);
        }

        public static async Task<bool> ChangePasswordAsync(int employeeID, string newPassword)
        {
            // --- LOGIC MÃ HÓA MD5 TRỰC TIẾP TẠI ĐÂY ---
            string hashedPassword = "";
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(newPassword);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                hashedPassword = sb.ToString();
            }
            // ------------------------------------------

            // Truyền chuỗi đã mã hóa xuống Database
            return await employeeDB.ChangePasswordAsync(employeeID, hashedPassword);
        }


    }
}
