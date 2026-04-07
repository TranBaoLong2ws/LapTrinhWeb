using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020656.DataLayers.Interfaces;
using SV22T1020656.Models.DataDictionary;
using System.Data;

namespace SV22T1020656
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy xuất dữ liệu
    /// đối với bảng Provinces trong cơ sở dữ liệu.
    /// </summary>
    public class ProvinceRepository : IDataDictionaryRepository<Province>
    {
        /// <summary>
        /// Chuỗi kết nối đến cơ sở dữ liệu
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo đối tượng ProvinceRepository
        /// </summary>
        /// <param name="connectionString">
        /// Chuỗi kết nối đến cơ sở dữ liệu SQL Server
        /// </param>
        public ProvinceRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách tỉnh thành từ bảng Provinces
        /// </summary>
        /// <returns>
        /// Danh sách các đối tượng <see cref="Province"/>
        /// </returns>
        public async Task<List<Province>> ListAsync()
        {
            List<Province> data = new List<Province>();

            using (IDbConnection connection = new SqlConnection(_connectionString))
            {
                string sql = @"SELECT ProvinceName 
                               FROM Provinces
                               ORDER BY ProvinceName";

                var result = await connection.QueryAsync<Province>(sql);
                data = result.ToList();
            }

            return data;
        }
    }
}