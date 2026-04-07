using Microsoft.AspNetCore.Mvc;
using SV22T1020656.Admin;
using SV22T1020656.BusinessLayers;
using SV22T1020656.Models.Common;
using SV22T1020656.Models.Partner;
using System.Threading.Tasks;

namespace _22T1020656.Admin.Controllers
{
    public class CustomerController : Controller
    {


        
        private const String customer_search = "CustomerSearchInput";


        /// <summary>
        /// hiện thị danh sách khách hàng nhập đầu vào tìm kiếm -> hiển thị kết quả tìm kiếm 
        /// </summary>
        /// <returns></returns>
        public  IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(customer_search);
            if (input == null)
             input = new PaginationSearchInput()
            {
                Page = 1,
                PageSize = ApplicationContext.PageSize,
                SearchValue = ""
            };

            return View(input);
        }

        /// <summary>
        /// tìm kiếm và trả về kết quả
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await PartnerDataService.ListCustomersAsync(input);

            ApplicationContext.SetSessionData(customer_search, input);
            
            return View(result);
        }


        /// <summary>
        /// tạo mới khách hàng 
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Thêm khách hàng";
            var model = new Customer()
            {
                CustomerID = 0
            };
            return View(new Customer());
        }


        /// <summary>
        /// cập nhập thông tin khách hàng
        /// </summary>
        /// <param name="id">mã khách hàng cần cập nhập</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật khách hàng";

            var customer = await PartnerDataService.GetCustomerAsync(id);

            if (customer == null)
                return RedirectToAction("Index");


            return View(customer);
        }




        [HttpPost]
        public async Task<IActionResult> SaveData(Customer customer)
        {
            try
            {
                ViewBag.Title = customer.CustomerID == 0 ? "Thêm khách hàng" : "Cập nhật khách hàng";

                // Load dropdown
                ViewBag.Provinces = await SelectListHelper.Provinces();

                // Validate
                if (String.IsNullOrWhiteSpace(customer.CustomerName))
                    ModelState.AddModelError("CustomerName", "Vui lòng nhập tên khách hàng");

                if (String.IsNullOrWhiteSpace(customer.Email))
                    ModelState.AddModelError(nameof(customer.Email), "Email không được để trống");
                else
                {
                    bool isExist = await PartnerDataService.ValidateAsync(customer.Email, customer.CustomerID);
                    if (!isExist)
                        ModelState.AddModelError(nameof(customer.Email), "Email này bị trùng");
                }

                if (String.IsNullOrWhiteSpace(customer.Province))
                    ModelState.AddModelError(nameof(customer.Province), "Vui lòng chọn tỉnh thành");

                // Nếu lỗi → quay lại Edit
                if (!ModelState.IsValid)
                {
                    ViewBag.Provinces = await SelectListHelper.Provinces();

                    ModelState.Remove(nameof(customer.Province)); // 🔥 FIX CHÍNH

                    return View("Edit", customer);
                }

                // Chuẩn hóa dữ liệu
                if (String.IsNullOrWhiteSpace(customer.ContactName))
                    customer.ContactName = customer.CustomerName;

                if (String.IsNullOrEmpty(customer.Phone))
                    customer.Phone = "";

                if (String.IsNullOrEmpty(customer.Address))
                    customer.Address = "";

                // Thêm hoặc cập nhật
                if (customer.CustomerID == 0)
                {
                    await PartnerDataService.AddCustomerAsync(customer);
                }
                else
                {
                    await PartnerDataService.UpdateCustomerAsync(customer);
                }

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                ModelState.AddModelError("Error", "Hệ thông đang lỗi, vui lòng thử lại sau");
                ViewBag.Provinces = await SelectListHelper.Provinces();
                return View("Edit", customer);
            }
        }

        /// <summary>
        /// xóa khách hàng
        /// </summary>
        /// <param name="id">mã khách hàng cần xóa </param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await PartnerDataService.DeleteCustomerAsync(id);
                return RedirectToAction("Index");
            }


            var model = await PartnerDataService.GetCustomerAsync(id);

            if (model == null)
            {
                return RedirectToAction("Index");
            }

            ViewBag.CanDelete = !await PartnerDataService.IsUsedCustomer(id);

            return View(model);
        }


        /// <summary>
        /// Giao diện đổi mật khẩu (GET)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null) return RedirectToAction("Index");

            ViewBag.Title = "Đổi mật khẩu khách hàng";
            return View(model);
        }

        /// <summary>
        /// Thực hiện lưu mật khẩu (POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(int id, string newPassword, string confirmPassword)
        {
            var customer = await PartnerDataService.GetCustomerAsync(id);
            if (customer == null) return RedirectToAction("Index");

            // --- KIỂM TRA VALIDATION ---
            if (string.IsNullOrWhiteSpace(newPassword))
                ModelState.AddModelError("newPassword", "Vui lòng nhập mật khẩu mới.");

            if (newPassword != confirmPassword)
                ModelState.AddModelError("confirmPassword", "Xác nhận mật khẩu không khớp.");

            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            // --- THỰC HIỆN ĐỔI MẬT KHẨU CÓ MÃ HÓA ---
            // QUAN TRỌNG: Mã hóa mật khẩu mới sang MD5 trước khi truyền vào Service
            string encryptedPassword = CryptHelper.HashMD5(newPassword);

            bool isUpdated = await PartnerDataService.ChangeCustomerPasswordAsync(id, encryptedPassword);

            if (isUpdated)
            {
                TempData["PasswordSuccess"] = $"Đổi mật khẩu cho khách hàng {customer.CustomerName} thành công!";
                return RedirectToAction("ChangePassword", new { id = id });
            }
            else
            {
                ModelState.AddModelError("", "Lỗi hệ thống: Không thể cập nhật dữ liệu mật khẩu mới.");
                return View(customer);
            }
        }

    }

    
}
