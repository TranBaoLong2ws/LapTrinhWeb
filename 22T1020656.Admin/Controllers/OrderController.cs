using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SV22T1020656.BusinessLayers;
using SV22T1020656.Models.Catalog;
using SV22T1020656.Models.Partner;
using SV22T1020656.Models.Sales;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;

namespace SV22T1020656.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class OrderController : Controller
    {

        

        /// <summary>
        /// Giao diện danh sách đơn hàng
        /// </summary>
        /// 
        private const int PAGESIZE = 10;
        private const string ORDER_SEARCH = "OrderSearchInput";

    

        public IActionResult Index()
        {

            var input = ApplicationContext.GetSessionData<OrderSearchInput>(ORDER_SEARCH);
            if (input == null)
            {
                input = new OrderSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = "",
                    Status = 0, 
                    DateFrom = null,
                    DateTo = null
                };
            }
            return View(input);
        }


        /// <summary>
        /// Tìm kiếm, lọc và phân trang đơn hàng
        /// </summary>
        public async Task<IActionResult> Search(OrderSearchInput input)
        {
            // 1. Xử lý khoảng ngày từ chuỗi "dd/MM/yyyy - dd/MM/yyyy"
            //if (!string.IsNullOrEmpty(dateRange))
            //{
            //    var dates = dateRange.Split(" - ");
            //    if (dates.Length == 2)
            //    {
            //        input.DateFrom = DateTime.ParseExact(dates[0], "dd/MM/yyyy", CultureInfo.InvariantCulture);
            //        input.DateTo = DateTime.ParseExact(dates[1], "dd/MM/yyyy", CultureInfo.InvariantCulture);
            //    }
            //}

            input.SearchValue ??= "";

            // 2. Gọi Service thực tế từ SalesDataService
            var result = await SalesDataService.ListOrdersAsync(input);

            // 3. Lưu lại điều kiện tìm kiếm vào Session
            ApplicationContext.SetSessionData(ORDER_SEARCH, input);

            return PartialView(result);
        }

        /// <summary>
        /// Giao diện lập đơn hàng mới
        /// </summary>

        public async Task<IActionResult> SearchProduct(ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductAsync(input);
            ApplicationContext.SetSessionData(ORDER_SEARCH, input);
            return View(result);

        }

        public IActionResult ShowCart()
        {
            var cart = ShoppingCartService.GetShoppingCart();
            return View(cart);
        }

        public async Task<IActionResult> AddCartItem(int productID, int quantity, decimal price)
        {
            if (quantity <= 0)
                return Json(new ApiResult(0, "Số lượng không hợp lệ"));
            if (price < 0)
                return Json(new ApiResult(0, "Giá bán không hợp lệ"));

            var product = await CatalogDataService.GetProductAsync(productID);
            if (product == null)
                return Json(new ApiResult(0, "Mặt hàng không tồn tại"));
            if (!product.IsSelling)
                return Json(new ApiResult(0, "Mặt hàng đã ngừng bán"));

            var item = new OrderDetailViewInfo()
            {
                ProductID = productID,
                Quantity = quantity,
                SalePrice = price,
                ProductName = product.ProductName,
                Unit = product.Unit,
                Photo = product.Photo ?? "nophoto.png"
            };
            ShoppingCartService.AddCartItem(item);

            return Json(new ApiResult(1));
        }
        public IActionResult Create()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(ORDER_SEARCH);
            if (input == null)
            {
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = 3,
                    SearchValue = "",
                };
            }
            return View(input);
        }


  

        /// <summary>
        /// xem chi tiết đơn hàng
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Detail(int id)
        {
            
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return RedirectToAction("Index");

           
            var details = await SalesDataService.ListOrderDetailsAsync(id);

           
            var model = new Tuple<OrderViewInfo, List<OrderDetailViewInfo>>(order, details);

            return View(model);
        }

        /// <summary>
        /// cập nhập sản phẩm trong giỏ hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult EditCartItem(int productID, int quantity, decimal salePrice)
        {
            if (quantity <= 0)
                return Json(new { isSuccess = false, message = "Số lượng không hợp lệ" });

            // Gọi Service 
            ShoppingCartService.UpdateCartItem(productID, quantity, salePrice);

            // Trả về object có thuộc tính isSuccess 
            return Json(new { isSuccess = true });
        }

        //public IActionResult updateCartItem(int productID, int quantity, decimal salePrice)
        //{
        //    if (quantity <= 0)
        //    {
        //        return Json(new ApiResult(0,"Số lượng không hợp lệ"));
        //    }

        //    if (salePrice <0 )
        //    {

        //    }

        //}

        public IActionResult UpdateCartItem(int productId, int quantity, decimal salePrice)
        {
            if (quantity <= 0)
                return Json(new ApiResult(0, "Số lượng không hợp lệ"));


            ShoppingCartService.UpdateCartItem(productId, quantity, salePrice);
            return Json(new ApiResult(1));
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(int customerID = 0, string province = "", string address = "")
        {
            var cart = ShoppingCartService.GetShoppingCart();
            if (cart.Count == 0)
                return Json(new ApiResult(0, "Giỏ hàng trống"));

            // Kiểm tra 
            if (customerID <= 0)
                return Json(new ApiResult(0, "Vui lòng chọn khách hàng"));

            var order = new Order()
            {
                CustomerID = customerID,
                DeliveryProvince = province,
                DeliveryAddress = address,
                OrderTime = DateTime.Now, 
                Status = OrderStatusEnum.New
            };

            
            int orderID = await SalesDataService.AddOrderAsync(order);

            if (orderID > 0)
            {
                foreach (var item in cart)
                {
                    await SalesDataService.AddOrderDetailAsync(new OrderDetail()
                    {
                        OrderID = orderID,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        SalePrice = item.SalePrice
                    });
                }
                ShoppingCartService.ClearCart();

             
                return Json(new { code = 1, data = orderID });
            }

            return Json(new ApiResult(0, "Không thể lưu đơn hàng vào cơ sở dữ liệu"));
        }
        /// <summary>
        /// xóa sản phẩm trong giỏ hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult DeleteCartItem(int productID = 0 )
        {
            if (Request.Method == "POST")
            {
                ShoppingCartService.RemoveCartItem(productID);
                return Json(new ApiResult(1));
            }

            var item = ShoppingCartService.GetCartItem(productID);
            return PartialView(item);
        }

        /// <summary>
        /// xóa sản phẩm trong giỏ hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult ClearCart()
        {
            if (Request.Method == "POST")
            {
                ShoppingCartService.ClearCart();
                return Json(new ApiResult(1));
            }
            return PartialView();
        }

        /// <summary>
        /// hiện thị trạng thái xác nhận đơn hàng
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Accept(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null) return NotFound();

            if (Request.Method == "POST")
            {
                if (order.Status != OrderStatusEnum.New)
                {
                   
                    TempData["Error"] = "Chỉ đơn hàng mới mới được phép duyệt.";
                    return RedirectToAction("Detail", new { id = id });
                }

                bool result = await SalesDataService.AcceptOrderAsync(id, 1);

                if (result)
                    TempData["Success"] = "Duyệt đơn hàng thành công!";
                else
                    TempData["Error"] = "Lỗi khi duyệt đơn.";

              
                return RedirectToAction("Detail", new { id = id });
            }
            return View(order);
        }

        /// <summary>
        /// hiện thị cập nhập trạng thái đơn hàng (đang vận chuyển)
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Shipping(int id, int shipperID = 0)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null) return NotFound();

            if (Request.Method == "POST")
            {
                // Kiểm tra điều kiện logic
                if (order.Status != OrderStatusEnum.Accepted)
                {
                    TempData["Error"] = "Đơn hàng phải được duyệt trước khi giao.";
                    return RedirectToAction("Detail", new { id = id });
                }

                
                bool result = await SalesDataService.ShipOrderAsync(id, shipperID);

                if (result)
                    TempData["Success"] = "Chuyển giao đơn hàng cho người giao hàng thành công!";
                else
                    TempData["Error"] = "Lỗi khi thực hiện chuyển giao hàng.";

                return RedirectToAction("Detail", new { id = id });
            }
            return View(order);
        }

        /// <summary>
        /// HIện thị trnang thái hoàn tất đơn hàng
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Finish(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null) return NotFound();

            if (Request.Method == "POST")
            {
             
                if ((int)order.Status != 3)
                {
                    TempData["Error"] = "Đơn hàng phải ở trạng thái đang giao mới có thể hoàn tất.";
                    return RedirectToAction("Detail", new { id = id });
                }

                bool result = await SalesDataService.CompleteOrderAsync(id);

                if (result)
                    TempData["Success"] = "Đơn hàng đã được hoàn tất thành công!";
                else
                    TempData["Error"] = "Có lỗi xảy ra khi cập nhật trạng thái hoàn tất.";

               
                return RedirectToAction("Detail", new { id = id });
            }
            return View(order);
        }


        /// <summary>
        /// Hiện thị trạng thái từ chối đơn hàng
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Reject(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null) return NotFound();

            if (Request.Method == "POST")
            {
                
                if (order.Status == OrderStatusEnum.Completed ||
                    order.Status == OrderStatusEnum.Cancelled)
                {
                    TempData["Error"] = "Không thể từ chối đơn hàng này.";
                    return RedirectToAction("Detail", new { id });
                }

                bool result = await SalesDataService.RejectOrderAsync(id, 1);

                if (result)
                    TempData["Success"] = "Đơn hàng đã bị từ chối!";
                else
                    TempData["Error"] = "Lỗi khi từ chối đơn hàng.";

              
                return RedirectToAction("Detail", new { id });
            }

            return View(order);
        }

        /// <summary>
        /// hiện thị trạng thái hủy đơn hàng
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null) return NotFound();

            if (Request.Method == "POST")
            {
                if (order.Status == OrderStatusEnum.Completed)
                {
                    TempData["Error"] = "Đơn hàng đã hoàn tất, không thể hủy.";
                    return RedirectToAction("Details", new { id = id });
                }

                bool result = await SalesDataService.CancelOrderAsync(id);
                if (result)
                {
                    TempData["Message"] = "Hủy đơn hàng thành công.";
                }
                else
                {
                    TempData["Error"] = "Lỗi khi hủy đơn.";
                }

              
                return RedirectToAction("Detail", new { id = id });
            }

            return View(order);
        }

        /// <summary>
        /// hiện thị trạng thái xác nhận xóa đơn hàng 
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null) return NotFound();

            if (Request.Method == "POST")
            {
               
                if (order.Status != OrderStatusEnum.New &&
                    order.Status != OrderStatusEnum.Cancelled &&
                    order.Status != OrderStatusEnum.Rejected)
                {
                    TempData["Error"] = "Không được phép xóa đơn hàng đang xử lý.";
                    return RedirectToAction("Detail", new { id });
                }

                bool result = await SalesDataService.DeleteOrderAsync(id);

                if (result)
                {
                    TempData["Success"] = "Xóa đơn hàng thành công!";

                   
                   
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Error"] = "Lỗi khi xóa.";
                    return RedirectToAction("Detail", new { id });
                }
            }

            return View(order);
        }


    }
}
