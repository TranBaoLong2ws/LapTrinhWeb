using SV22T1020656.DataLayers.Interfaces;
using SV22T1020656.Models.Catalog;
using SV22T1020656.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020656.BusinessLayers
{
    public static class CatalogDataService
    {
        private static readonly IGenericRepository<Category> categoryDB;

        private static readonly IProductRepository productDB;



        static CatalogDataService()
        {
            categoryDB = new CategoryRepository(Configuration.ConnectionString);
            productDB = new ProductRepository(Configuration.ConnectionString);
        }

        // các chức năng nghiệp vụ liên quan đến Category



        /// <summary>
        /// Tìm kiếm và trả về danh sách các danh mục sản phẩm dưới dạng phân trang 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static async Task<PagedResult<Category>> ListCategory(PaginationSearchInput input)
        {
            return await categoryDB.ListAsync(input);
        }


        /// <summary>
        /// lấy thông tin của một danh mục sản phẩm dựa vào mã danh mục sản phẩm
        /// </summary>
        /// <param name="supplierID"></param>
        /// <returns></returns>
        public static async Task<Category> GetCategoryAsync(int category)
        {
            return await categoryDB.GetAsync(category);
        }


        /// <summary>
        /// bổ sung danh mục sản phẩm
        /// </summary>
        /// <param name="supplier"></param>
        /// <returns>mã nhà cung cấp được bổ sung</returns>
        public static async Task<int> AddCategoryAsync(Category category)
        {
            return await categoryDB.AddAsync(category);
        }


        /// <summary>
        /// cập nhập thông tin danh mục sản phẩm
        /// </summary>
        /// <param name="supplier"></param>
        /// <returns></returns>
        public static async Task<bool> UpdateCategoryAsync(Category category)
        {
            return await categoryDB.UpdateAsync(category);
        }

        /// <summary>
        /// xóa danh mục sản phẩm
        /// </summary>
        /// <param name="supplier"></param>
        /// <returns></returns>
        public static async Task<bool> DeleteCategoryAsync(int id)
        {
            return await categoryDB.DeleteAsync(id);
        }





        // các chức năng nghiệp vụ liên quan đến product

        /// <summary>
        /// Tìm kiếm và trả về danh sách mặt hàng dưới dạng phân trang
        /// </summary>
        /// <param name="input">Điều kiện tìm kiếm</param>
        /// <returns></returns>
        public static async Task<PagedResult<Product>> ListProductAsync(ProductSearchInput input)
        {
            return await productDB.ListAsync(input);
        }


        /// <summary>
        /// Lấy thông tin chi tiết của một mặt hàng
        /// </summary>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns></returns>
        public static async Task<Product?> GetProductAsync(int productID)
        {
            return await productDB.GetAsync(productID);
        }

        /// <summary>
        /// Bổ sung mặt hàng mới
        /// </summary>
        /// <param name="data">Thông tin mặt hàng</param>
        /// <returns>Mã mặt hàng được bổ sung</returns>

        public static async Task<int> AddProductAsync(Product data)
        {
            return await productDB.AddAsync(data);
        }



        /// <summary>
        /// Cập nhật thông tin mặt hàng
        /// </summary>
        /// <param name="data">Thông tin mặt hàng</param>
        /// <returns></returns>
        public static async Task<bool> UpdateProductAsync(Product data)
        {
            return await productDB.UpdateAsync(data);
        }


        /// <summary>
        /// Xóa mặt hàng
        /// </summary>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns></returns>
        public static async Task<bool> DeleteProductAsync(int productID)
        {
            if (await productDB.IsUsedAsync(productID))
                return false;

            return await productDB.DeleteAsync(productID);
        }

        /// <summary>
        /// Lấy danh sách thuộc tính của mặt hàng
        /// </summary>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns></returns>
        public static async Task<List<ProductAttribute>> ListProductAttributesAsync(int productID)
        {
            return await productDB.ListAttributesAsync(productID);
        }


        /// <summary>
        /// Lấy thông tin của một thuộc tính
        /// </summary>
        /// <param name="attributeID"></param>
        /// <returns></returns>
        public static async Task<ProductAttribute?> GetProductAttributeAsync(long attributeID)
        {
            return await productDB.GetAttributeAsync(attributeID);
        }


        /// <summary>
        /// Bổ sung thuộc tính cho mặt hàng
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static async Task<long> AddProductAttributeAsync(ProductAttribute data)
        {
            return await productDB.AddAttributeAsync(data);
        }


        /// <summary>
        /// Cập nhật thuộc tính của mặt hàng
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static async Task<bool> UpdateProductAttributeAsync(ProductAttribute data)
        {
            return await productDB.UpdateAttributeAsync(data);
        }


        /// <summary>
        /// Xóa thuộc tính của mặt hàng
        /// </summary>
        /// <param name="attributeID"></param>
        /// <returns></returns>
        public static async Task<bool> DeleteProductAttributeAsync(long attributeID)
        {
            return await productDB.DeleteAttributeAsync(attributeID);
        }



        /// <summary>
        /// Lấy danh sách ảnh của mặt hàng
        /// </summary>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns></returns>
        public static async Task<List<ProductPhoto>> ListProductPhotosAsync(int productID)
        {
            return await productDB.ListPhotosAsync(productID);
        }


        /// <summary>
        /// Lấy thông tin một ảnh của mặt hàng
        /// </summary>
        /// <param name="photoID"></param>
        /// <returns></returns>
        public static async Task<ProductPhoto?> GetProductPhotoAsync(long photoID)
        {
            return await productDB.GetPhotoAsync(photoID);
        }


        /// <summary>
        /// Bổ sung ảnh cho mặt hàng
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static async Task<long> AddProductPhotoAsync(ProductPhoto data)
        {
            return await productDB.AddPhotoAsync(data);
        }


        /// <summary>
        /// Cập nhật ảnh của mặt hàng
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static async Task<bool> UpdateProductPhotoAsync(ProductPhoto data)
        {
            return await productDB.UpdatePhotoAsync(data);
        }


        /// <summary>
        /// Xóa ảnh của mặt hàng
        /// </summary>
        /// <param name="photoID"></param>
        /// <returns></returns>
        public static async Task<bool> DeleteProductPhotoAsync(long photoID)
        {
            return await productDB.DeletePhotoAsync(photoID);
        }


        public static async Task<bool> IsIsUsed(int id)
        {
            return await categoryDB.IsUsed(id);
        }


    }
}
