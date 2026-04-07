using SV22T1020656;
using SV22T1020656.BusinessLayers;
using SV22T1020656.DataLayers.Interfaces;
using SV22T1020656.Models.Common;
using SV22T1020656.Models.Sales;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020656.BusinessLayers
{
    public static class SalesDataService
    {
        /// <summary>
        /// Đối tượng truy xuất dữ liệu đơn hàng
        /// </summary>
        private static readonly IOrderRepository orderDB;


        static SalesDataService()
        {
            /// <summary>
            /// Hàm khởi tạo tĩnh của lớp SalesDataService
            /// </summary>
            orderDB = new OrderRepository(Configuration.ConnectionString);
        }

        /// <summary>
        /// Tìm kiếm và trả về danh sách đơn hàng dưới dạng phân trang
        /// </summary>
        /// <param name="input">Điều kiện tìm kiếm</param>
        /// <returns></returns>
        public static async Task<PagedResult<OrderViewInfo>> ListOrdersAsync(OrderSearchInput input)
        {
            return await orderDB.ListAsync(input);
        }


        /// <summary>
        /// Lấy thông tin chi tiết của một đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <returns></returns>
        public static async Task<OrderViewInfo?> GetOrderAsync(int orderID)
        {
            return await orderDB.GetAsync(orderID);
        }


        /// <summary>
        /// Tạo mới đơn hàng
        /// </summary>
        /// <param name="data">Thông tin đơn hàng</param>
        /// <returns>Mã đơn hàng được tạo</returns>
        public static async Task<int> AddOrderAsync(Order data)
        {
            return await orderDB.AddAsync(data);
        }


        /// <summary>
        /// Cập nhật thông tin đơn hàng
        /// </summary>
        /// <param name="data">Thông tin đơn hàng</param>
        /// <returns></returns>
        public static async Task<bool> UpdateOrderAsync(Order data)
        {
            return await orderDB.UpdateAsync(data);
        }


        /// <summary>
        /// Xóa đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <returns></returns>
        public static async Task<bool> DeleteOrderAsync(int orderID)
        {
            return await orderDB.DeleteAsync(orderID);
        }


        /// <summary>
        /// Lấy danh sách các mặt hàng trong đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <returns></returns>
        public static async Task<List<OrderDetailViewInfo>> ListOrderDetailsAsync(int orderID)
        {
            return await orderDB.ListDetailsAsync(orderID);
        }


        /// <summary>
        /// Lấy thông tin chi tiết của một mặt hàng trong đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns></returns>
        public static async Task<OrderDetailViewInfo?> GetOrderDetailAsync(int orderID, int productID)
        {
            return await orderDB.GetDetailAsync(orderID, productID);
        }


        /// <summary>
        /// Bổ sung mặt hàng vào đơn hàng
        /// </summary>
        /// <param name="data">Thông tin mặt hàng</param>
        /// <returns></returns>
        public static async Task<bool> AddOrderDetailAsync(OrderDetail data)
        {
            return await orderDB.AddDetailAsync(data);
        }



        /// <summary>
        /// Cập nhật thông tin mặt hàng trong đơn hàng
        /// </summary>
        /// <param name="data">Thông tin mặt hàng</param>
        /// <returns></returns>
        public static async Task<bool> UpdateOrderDetailAsync(OrderDetail data)
        {
            return await orderDB.UpdateDetailAsync(data);
        }


        /// <summary>
        /// Xóa mặt hàng khỏi đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns></returns>
        public static async Task<bool> DeleteOrderDetailAsync(int orderID, int productID)
        {
            return await orderDB.DeleteDetailAsync(orderID, productID);
        }

        /// <summary>
        /// Duyệt đơn hàng
        /// </summary>
        public static async Task<bool> AcceptOrderAsync(int orderID, int employeeID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                return false;

            if (order.Status != OrderStatusEnum.New)
                return false;

            order.EmployeeID = employeeID;
            order.AcceptTime = DateTime.Now;
            order.Status = OrderStatusEnum.Accepted;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Giao đơn hàng cho người giao hàng
        /// </summary>
        public static async Task<bool> ShipOrderAsync(int orderID, int shipperID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                return false;

            if (order.Status != OrderStatusEnum.Accepted)
                return false;

            order.ShipperID = shipperID;
            order.ShippedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Shipping;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Hoàn tất đơn hàng
        /// </summary>
        public static async Task<bool> CompleteOrderAsync(int orderID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                return false;

            if (order.Status != OrderStatusEnum.Shipping)
                return false;

            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Completed;

            return await orderDB.UpdateAsync(order);
        }


        /// <summary>
        /// Từ chối đơn hàng
        /// </summary>
        public static async Task<bool> RejectOrderAsync(int orderID, int employeeID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                return false;

            if (order.Status != OrderStatusEnum.New)
                return false;

            order.EmployeeID = employeeID;
            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Rejected;

            return await orderDB.UpdateAsync(order);
        }


        /// <summary>
        /// Hủy đơn hàng
        /// </summary>
        public static async Task<bool> CancelOrderAsync(int orderID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                return false;

            if (order.Status != OrderStatusEnum.New &&
                order.Status != OrderStatusEnum.Accepted)
                return false;

            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Cancelled;

            return await orderDB.UpdateAsync(order);
        }

    }
}
