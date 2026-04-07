using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020656.BusinessLayers
{   
    /// <summary>
    /// lớp giữ các thông tin cấu hình sử dụng cho BusinessLayer
    /// </summary>
    public static class Configuration
    {
        private static String _connectionString = "";


        /// <summary>
        /// Khởi tạo cấu hình cho BusinessLayer
        /// hàm này gọi nó trước khi chạy ứng dụng
        /// </summary>
        /// <param name="connectionString"></param>
        public static void Initialize(String connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// lấy chuổi tham số kết nối đến CSDL sử dụng trong hệ thống
        /// </summary>
        public static String ConnectionString => _connectionString;







    }
}
