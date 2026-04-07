using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020656.BusinessLayers;
using SV22T1020656.BusinessLayersrs;
using SV22T1020656.Models.Cart;
using SV22T1020656.Models.Sales;
using SV22T1020656.Shop;
using System.Threading.Tasks;

namespace SV22T1020656.Shop.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private const string CART_SESSION = "ShoppingCart";
        

        public async Task<IActionResult> Index()
        {
            var cart = GetCart();
            ViewBag.Provinces = await DictionaryDataService.ListProvince();
            return View(cart);
        }

        public async Task<IActionResult> addToCart(int id, int quantity = 1)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductID == id);

            if (item == null)
            {
                // Nếu sản phẩm chưa có trong giỏ và số lượng gửi lên > 0 thì mới thêm mới
                if (quantity > 0)
                {
                    var product = await CatalogDataService.GetProductAsync(id);
                    if (product != null)
                    {
                        cart.Add(new cartItem
                        {
                            ProductID = product.ProductID,
                            ProductName = product.ProductName,
                            Photo = product.Photo,
                            Unit = product.Unit,
                            Price = product.Price,
                            Quantity = quantity 
                        });
                    }
                }
            }
            else
            {
                
                item.Quantity += quantity;

                
                if (item.Quantity <= 0)
                {
                    cart.Remove(item);
                }
            }

            SaveCart(cart);

           
            return Json(new
            {
                success = true,
                cartCount = cart.Count,
                message = "Thêm giỏ hàng thành công"
            });
        }

        private List<cartItem> GetCart()
        {
            var cart = ApplicationContext.GetSessionData<List<cartItem>>(CART_SESSION);
            return cart ?? new List<cartItem>();
        }

        private void SaveCart(List<cartItem> carts)
        {
            ApplicationContext.SetSessionData(CART_SESSION, carts);
        }


        public IActionResult RemoveFromCart(int id)
        {
            var cart = ApplicationContext.GetSessionData<List<cartItem>>(CART_SESSION);
            if (cart != null)
            {
                var item = cart.FirstOrDefault(x => x.ProductID == id);
                if (item != null)
                {
                    cart.Remove(item);
                    ApplicationContext.SetSessionData("ShoppingCart", cart);
                }
            }
            return Json(new { success = true });
        }

        private int GetCustomerID()
        {
            // Tìm đúng tên "UserId" mà bạn đã định nghĩa ở hàm Login
            var claim = User.FindFirst("UserId");

            if (claim != null && int.TryParse(claim.Value, out int id))
            {
                return id;
            }
            return 0; // Nếu không thấy sẽ trả về 0, dẫn đến bị Redirect về Login
        }


        [HttpPost]
        public async Task<IActionResult> InitOrder(string deliveryProvince, string deliveryAddress)
        {

            //return Content($"Đã vào hàm! Tỉnh: {deliveryProvince}, Địa chỉ: {deliveryAddress}");
            // 1. Lấy dữ liệu giỏ hàng từ Session
            var cart = GetCart();
            if (cart.Count == 0) return RedirectToAction("Index");

           
            // 1. Lấy CustomerID từ Claim "UserId"
            int customerId = GetCustomerID();
            if (customerId == 0) return RedirectToAction("Login", "Account");

            // 3. Khởi tạo đối tượng Order với Status từ Enum
            var orderData = new Order()
            {
                CustomerID = customerId,
                OrderTime = DateTime.Now,
                DeliveryProvince = deliveryProvince,
                DeliveryAddress = deliveryAddress,
                // Sử dụng Enum thay vì gõ số 1 trực tiếp
                Status = OrderStatusEnum.New,
                EmployeeID = null // Đơn mới chưa có nhân viên xử lý
            };

            // 4. Lưu đơn hàng vào bảng Orders qua SalesDataService
            // Hàm này sẽ trả về OrderID vừa được sinh ra (SCOPE_IDENTITY)
            int orderID = await SalesDataService.AddOrderAsync(orderData);

            if (orderID > 0)
            {
                // 5. Duyệt giỏ hàng để lưu vào bảng OrderDetails
                foreach (var item in cart)
                {
                    var detail = new OrderDetail()
                    {
                        
                        OrderID = orderID,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        SalePrice = item.Price // Giá bán tại thời điểm đặt hàng
                    };
                    await SalesDataService.AddOrderDetailAsync(detail);
                }

                // 6. Xóa giỏ hàng
                ClearCart();

                // 7. Chuyển hướng sang trang thông báo thành công (Mục 9)
                //return RedirectToAction("Index", new { id = orderID });
                return RedirectToAction("Success", new { id = orderID });
            }

            // Nếu lưu thất bại, quay lại trang giỏ hàng và báo lỗi
            ModelState.AddModelError("", "Không thể khởi tạo đơn hàng. Vui lòng thử lại.");
            return View("Index", cart);
        }



        private void ClearCart()
        {

            SaveCart(new List<cartItem>());

        }

        [HttpGet]
        // Đảm bảo người dùng phải đăng nhập mới được thanh toán
        public async Task<IActionResult> CheckOut()
        {
            var cart = GetCart();
            if (cart.Count == 0)
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index");
            }

            // Lấy danh sách tỉnh thành từ DB để khách chọn nơi giao
            // Giả sử bạn dùng CommonDataService hoặc ProvinceRepository
            ViewBag.Provinces = await DictionaryDataService.ListProvince();

            return View(cart);
        }


        [HttpGet]
        public async Task<IActionResult> Success(int id)
        {
            // Lấy thông tin đơn hàng kèm theo trạng thái hiện tại từ DB
            var order = await SalesDataService.GetOrderAsync(id);

            if (order == null) return RedirectToAction("Index", "Home");

            return View(order); // Truyền Model là Order vào View
        }


    }
}
