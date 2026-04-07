using SV22T1020656.BusinessLayers;
using SV22T1020656.DataLayers.Interfaces;
using SV22T1020656.Models.DataDictionary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020656.BusinessLayersrs
{
    public static class DictionaryDataService 
    {
        private static readonly IDataDictionaryRepository<Province> provinceDB;

           static DictionaryDataService()
        {
            provinceDB = new ProvinceRepository(Configuration.ConnectionString);
        }

        // các chức năng nghiệp vụ đến dữ liệu province


        /// <summary>
        /// Lấy danh sách tất cả các tỉnh/thành trong cơ sở dữ liệu
        /// </summary>
        /// <returns>
        /// Danh sách các đối tượng <see cref="Province"/>
        /// </returns>
        public static async Task<List<Province>> ListProvince()
        {
            return await provinceDB.ListAsync();
        } 






    }
}
