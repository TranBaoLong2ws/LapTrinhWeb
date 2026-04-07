using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020656.DataLayers.Interfaces;
using SV22T1020656.Models.Catalog;
using SV22T1020656.Models.Common;
using System.Data;

namespace SV22T1020656
{
    /// <summary>
    /// Lớp thực hiện truy xuất dữ liệu cho bảng Products,
    /// ProductAttributes và ProductPhotos
    /// </summary>
    public class ProductRepository : IProductRepository
    {
        /// <summary>
        /// Chuỗi kết nối cơ sở dữ liệu
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo repository
        /// </summary>
        /// <param name="connectionString"></param>
        public ProductRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Mở kết nối database
        /// </summary>
        private IDbConnection OpenConnection()
        {
            return new SqlConnection(_connectionString);
        }

        #region Product

        /// <summary>
        /// Tìm kiếm danh sách sản phẩm
        /// </summary>
        public async Task<PagedResult<Product>> ListAsync(ProductSearchInput input)
        {

            using var connection = OpenConnection();

            var result = new PagedResult<Product>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            var parameters = new DynamicParameters();
            string condition = "WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                condition += " AND ProductName LIKE @SearchValue";
                parameters.Add("@SearchValue", $"%{input.SearchValue}%");
            }

            if (input.CategoryID > 0)
            {
                condition += " AND CategoryID=@CategoryID";
                parameters.Add("@CategoryID", input.CategoryID);
            }

            if (input.SupplierID > 0)
            {
                condition += " AND SupplierID=@SupplierID";
                parameters.Add("@SupplierID", input.SupplierID);
            }

            // Lọc giá tối thiểu (Chỉ lọc nếu MinPrice > 0)
            if (input.MinPrice.HasValue && input.MinPrice > 0)
            {
                condition += " AND Price >= @MinPrice";
                parameters.Add("@MinPrice", input.MinPrice);
            }

            // Lọc giá tối đa (Chỉ lọc nếu MaxPrice > 0)
            if (input.MaxPrice.HasValue && input.MaxPrice > 0)
            {
                condition += " AND Price <= @MaxPrice";
                parameters.Add("@MaxPrice", input.MaxPrice);
            }

            string countSQL = $@"
                SELECT COUNT(*)
                FROM Products
                {condition}";

            result.RowCount = await connection.ExecuteScalarAsync<int>(countSQL, parameters);

            string dataSQL = $@"
                SELECT *
                FROM Products
                {condition}
                ORDER BY ProductName
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add("@Offset", input.Offset);
            parameters.Add("@PageSize", input.PageSize);

            var data = await connection.QueryAsync<Product>(dataSQL, parameters);
            result.DataItems = data.ToList();

            return result;
        }

        /// <summary>
        /// Lấy thông tin 1 sản phẩm
        /// </summary>
        public async Task<Product?> GetAsync(int productID)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT * 
                           FROM Products
                           WHERE ProductID=@ProductID";

            return await connection.QueryFirstOrDefaultAsync<Product>(sql, new { productID });
        }

        /// <summary>
        /// Thêm sản phẩm
        /// </summary>
        public async Task<int> AddAsync(Product data)
        {
            using var connection = OpenConnection();

            string sql = @"
            INSERT INTO Products
            (
                ProductName,
                ProductDescription,
                SupplierID,
                CategoryID,
                Unit,
                Price,
                Photo,
                IsSelling
            )
            VALUES
            (
                @ProductName,
                @ProductDescription,
                @SupplierID,
                @CategoryID,
                @Unit,
                @Price,
                @Photo,
                @IsSelling
            );
            SELECT SCOPE_IDENTITY();";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật sản phẩm
        /// </summary>
        public async Task<bool> UpdateAsync(Product data)
        {
            using var connection = OpenConnection();

            string sql = @"
                UPDATE Products SET
                    ProductName=@ProductName,
                    ProductDescription=@ProductDescription,
                    SupplierID=@SupplierID,
                    CategoryID=@CategoryID,
                    Unit=@Unit,
                    Price=@Price,
                    Photo=@Photo,
                    IsSelling=@IsSelling
                WHERE ProductID=@ProductID";

            return await connection.ExecuteAsync(sql, data) > 0;
        }

        /// <summary>
        /// Xóa sản phẩm
        /// </summary>
        public async Task<bool> DeleteAsync(int productID)
        {
            using var connection = OpenConnection();

            string sql = @"DELETE FROM Products WHERE ProductID=@productID";

            return await connection.ExecuteAsync(sql, new { productID }) > 0;
        }

        /// <summary>
        /// Kiểm tra sản phẩm có đang được dùng trong OrderDetails
        /// </summary>
        public async Task<bool> IsUsedAsync(int productID)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT COUNT(*) FROM OrderDetails WHERE ProductID=@productID";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { productID });

            return count > 0;
        }

        #endregion


        #region Attributes

        /// <summary>
        /// Lấy danh sách thuộc tính của sản phẩm
        /// </summary>
        public async Task<List<ProductAttribute>> ListAttributesAsync(int productID)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT *
                           FROM ProductAttributes
                           WHERE ProductID=@productID
                           ORDER BY DisplayOrder";

            var data = await connection.QueryAsync<ProductAttribute>(sql, new { productID });

            return data.ToList();
        }

        /// <summary>
        /// Lấy 1 thuộc tính
        /// </summary>
        public async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT *
                           FROM ProductAttributes
                           WHERE AttributeID=@attributeID";

            return await connection.QueryFirstOrDefaultAsync<ProductAttribute>(sql, new { attributeID });
        }

        /// <summary>
        /// Thêm thuộc tính
        /// </summary>
        public async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            using var connection = OpenConnection();

            string sql = @"
                INSERT INTO ProductAttributes
                (
                    ProductID,
                    AttributeName,
                    AttributeValue,
                    DisplayOrder
                )
                VALUES
                (
                    @ProductID,
                    @AttributeName,
                    @AttributeValue,
                    @DisplayOrder
                );
                SELECT SCOPE_IDENTITY();";

            return await connection.ExecuteScalarAsync<long>(sql, data);
        }

        /// <summary>
        /// Cập nhật thuộc tính
        /// </summary>
        public async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            using var connection = OpenConnection();

            string sql = @"UPDATE ProductAttributes
                           SET AttributeName=@AttributeName,
                               AttributeValue=@AttributeValue,
                               DisplayOrder=@DisplayOrder
                           WHERE AttributeID=@AttributeID";

            return await connection.ExecuteAsync(sql, data) > 0;
        }

        /// <summary>
        /// Xóa thuộc tính
        /// </summary>
        public async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            using var connection = OpenConnection();

            string sql = @"DELETE FROM ProductAttributes WHERE AttributeID=@attributeID";

            return await connection.ExecuteAsync(sql, new { attributeID }) > 0;
        }

        #endregion


        #region Photos

        /// <summary>
        /// Lấy danh sách ảnh sản phẩm
        /// </summary>
        public async Task<List<ProductPhoto>> ListPhotosAsync(int productID)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT *
                           FROM ProductPhotos
                           WHERE ProductID=@productID
                           ORDER BY DisplayOrder";

            var data = await connection.QueryAsync<ProductPhoto>(sql, new { productID });

            return data.ToList();
        }

        /// <summary>
        /// Lấy thông tin 1 ảnh
        /// </summary>
        public async Task<ProductPhoto?> GetPhotoAsync(long photoID)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT *
                           FROM ProductPhotos
                           WHERE PhotoID=@photoID";

            return await connection.QueryFirstOrDefaultAsync<ProductPhoto>(sql, new { photoID });
        }

        /// <summary>
        /// Thêm ảnh sản phẩm
        /// </summary>
        public async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            using var connection = OpenConnection();

            string sql = @"
                INSERT INTO ProductPhotos
                (
                    ProductID,
                    Photo,
                    Description,
                    DisplayOrder,
                    IsHidden
                )
                VALUES
                (
                    @ProductID,
                    @Photo,
                    @Description,
                    @DisplayOrder,
                    @IsHidden
                );
                SELECT SCOPE_IDENTITY();";

            return await connection.ExecuteScalarAsync<long>(sql, data);
        }

        /// <summary>
        /// Cập nhật ảnh
        /// </summary>
        public async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            using var connection = OpenConnection();

            string sql = @"UPDATE ProductPhotos
                           SET Photo=@Photo,
                               Description=@Description,
                               DisplayOrder=@DisplayOrder,
                               IsHidden=@IsHidden
                           WHERE PhotoID=@PhotoID";

            return await connection.ExecuteAsync(sql, data) > 0;
        }

        /// <summary>
        /// Xóa ảnh
        /// </summary>
        public async Task<bool> DeletePhotoAsync(long photoID)
        {
            using var connection = OpenConnection();

            string sql = @"DELETE FROM ProductPhotos WHERE PhotoID=@photoID";

            return await connection.ExecuteAsync(sql, new { photoID }) > 0;
        }

        #endregion
    }
}