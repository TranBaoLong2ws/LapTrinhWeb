using SV22T1020656.DataLayers.Interfaces;
using SV22T1020656.Models.Common;
using SV22T1020656.Models.Partner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Threading.Tasks;


namespace SV22T1020656.BusinessLayers
{
    /// <summary>
    /// Lớp cung cấp các chức năng tác nghiệp của hệ thống liên quan các đối tác của hệ thông
    /// bao gồm: Customer, Supplier, Shipper
    /// </summary>
    public static class PartnerDataService
    {
        private static readonly IGenericRepository<Supplier> supplierDB;

        private static readonly IGenericRepository<Shipper> shipperDB;

        private static readonly ICustomerRepository customerDB;

        static PartnerDataService()
        {
            supplierDB = new SupplierRepository(Configuration.ConnectionString);
            shipperDB = new ShipperRepository(Configuration.ConnectionString);
            customerDB = new CustomerRepository(Configuration.ConnectionString);
        }


        // Các chức năng nghiệp vụ liên quan đến nhà cung chấp 


        /// <summary>
        /// Tìm kiếm và trả về danh sách các nhà cung cấp dưới dạng phân trang 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static async Task<PagedResult<Supplier>> ListSuppliersAsync(PaginationSearchInput input)
        {
            return await supplierDB.ListAsync(input);
        }

        /// <summary>
        /// lấy thông tin của một nhà cung cấp dựa vào mã nhà cung cấp
        /// </summary>
        /// <param name="supplierID"></param>
        /// <returns></returns>
        public static async Task<Supplier?> GetSupplierAsync(int supplierID)
        {
            
            return await supplierDB.GetAsync(supplierID);
        }


        /// <summary>
        /// bổ sung nhà cung cấp 
        /// </summary>
        /// <param name="supplier"></param>
        /// <returns>mã nhà cung cấp được bổ sung</returns>
        public static async Task<int> AddSupplierAsync(Supplier supplier)
        {
            // todo: kiểm tra tính hợp lệ của dữ liệu trước khi bổ sung
            return await supplierDB.AddAsync(supplier);
        }



        /// <summary>
        /// cập nhập thông tin nhà cung cấp
        /// </summary>
        /// <param name="supplier"></param>
        /// <returns></returns>
        public static async Task<bool> UpdateSupplierAsync(Supplier supplier)
        {
            // todo: kiểm tra tính hợp lệ của dữ liệu trước khi cập nhập
            return await supplierDB.UpdateAsync(supplier);
        }

        /// <summary>
        /// xóa nhà cung cấp 
        /// </summary>
        /// <param name="supplier"></param>
        /// <returns></returns>
       public static async Task<bool> DeleteSupplierAsync(int supplier)
        {
            if (await supplierDB.IsUsed(supplier)) return false;

            return await supplierDB.DeleteAsync(supplier);
        }

        /// <summary>
        /// kiểm tra nhà cung cấp còn được sử dụng hay không
        /// </summary>
        /// <param name="supplier"></param>
        /// <returns></returns>
        public static async Task<bool> IsUsedSupplierAsync(int supplier)
        {
            return await supplierDB.IsUsed(supplier);
        }



     



        // các chức năng nghiệp vụ liên quan đến người giao hàng

        /// <summary>
        /// Tìm kiếm và trả về danh sách người giao hàng 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static async Task<PagedResult<Shipper>> ListShippersAsync(PaginationSearchInput i)
        {
            return await shipperDB.ListAsync(i);
        }


        /// <summary>
        /// xem thông tin chi tiết một người giao hàng
        /// </summary>
        /// <param name="ShipperID"></param>
        /// <returns></returns>
        public static async Task<Shipper> GetShipperAsync(int ShipperID)
        {
            return await shipperDB.GetAsync(ShipperID);
        }

        /// <summary>
        /// Thêm người giao hàng
        /// </summary>
        /// <param name="shipper"></param>
        /// <returns></returns>
        public static async Task<int> AddShipperAsync(Shipper shipper)
        {
            return await shipperDB.AddAsync(shipper);
        }

        /// <summary>
        /// cập nhật thông tin người giao hàng
        /// </summary>
        /// <param name="shipper"></param>
        /// <returns></returns>
        public static async Task<bool> UpdateShipperAsync(Shipper shipper)
        {
            return await shipperDB.UpdateAsync(shipper);
        }

        /// <summary>
        /// xóa người giao hàng
        /// </summary>
        /// <param name="Shipper"></param>
        /// <returns></returns>
        public static async Task<bool> DeleteShipperAsync(int Shipper)
        {
            if (await shipperDB.IsUsed(Shipper)) return false;

            return await shipperDB.DeleteAsync(Shipper);
        }


        /// <summary>
        /// kiểm tra xem người giao hàng còn hoạt động hay không
        /// </summary>
        /// <param name="Shipper"></param>
        /// <returns></returns>
        public static async Task<bool> IsUsedShipperAsync(int Shipper)
        {
            return await shipperDB.IsUsed(Shipper);
        }




        // các chức năng nghiệp vụ liên quan đến khách hàng

        /// <summary>
        /// Tìm kiếm và trả về danh sách khách hàng 
        /// </summary>
        /// <param name="input1"></param>
        /// <returns></returns>
        public static async Task<PagedResult<Customer>> ListCustomersAsync(PaginationSearchInput input1)
        {
            return await customerDB.ListAsync(input1);
        }


        /// <summary>
        /// Lấy thông tin chi tiết của một nhân viên 
        /// </summary>
        /// <param name="customer"></param>
        /// <returns></returns>
        public static async Task<Customer> GetCustomerAsync(int customer) 
        {
            return await customerDB.GetAsync(customer);
        }

        /// <summary>
        /// Bổ sung khách hàng
        /// </summary>
        /// <param name="customer"></param>
        /// <returns></returns>
        public static async Task<int> AddCustomerAsync(Customer customer)
        {
            return await customerDB.AddAsync(customer);
        }


        /// <summary>
        /// Cập nhập thông tin khách hàng
        /// </summary>
        /// <param name="customer"></param>
        /// <returns></returns>
        public static async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            return await customerDB.UpdateAsync(customer);
        }


        /// <summary>
        /// Xóa thông tin khách hàng
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static async Task<bool> DeleteCustomerAsync(int id)
        {
            return await customerDB.DeleteAsync(id);
        }

        public static async Task<bool> IsUsedCustomer(int id)
        {
            return await customerDB.IsUsed(id);
        }


        /// <summary>
        /// Kiểm tra xem một địa chỉ email có hợp lệ hay không?
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">
        /// Nếu id = 0: Kiểm tra email của khách hàng mới.
        /// Nếu id <> 0: Kiểm tra email đối với khách hàng đã tồn tại
        /// </param>
        /// <returns></returns>
        public static async Task<bool> ValidateAsync(String email, int id )
        {
            return await customerDB.ValidateEmailAsync(email,id);
        }

        public static async Task<bool> ChangeCustomerPasswordAsync(int customerID, string newPassword)
        {
            // Bạn có thể xử lý mã hóa mật khẩu tại đây nếu lớp Repository chỉ làm nhiệm vụ lưu trữ
            // ví dụ: string hashedPassword = EncodePassword(newPassword);

            return await customerDB.ChangePasswordAsync(customerID, newPassword);
        }

    }
}
