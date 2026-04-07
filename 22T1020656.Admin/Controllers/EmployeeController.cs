using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020656.Admin;
using SV22T1020656.BusinessLayers;
using SV22T1020656.Models.Common;
using SV22T1020656.Models.HR;
using SV22T1020656.Models.Partner;
using System.Threading.Tasks;

namespace SV22T1020656.Admin.Controllers
{
    [Authorize(Roles = WebUserRoles.Administrator)]
    public class EmployeeController : Controller
    {

        private const String customer_search = "EmployeeSearchInput";


        /// <summary>
        /// hiện thị danh sách nhân viên
        /// </summary>
        /// <returns></returns>

        public IActionResult Index()
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
            var result = await HrDataService.ListEmployeeAsync(input);

            ApplicationContext.SetSessionData(customer_search, input);

            return View(result);
        }



        /// <summary>
        /// tạo mới nhân viên
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhân viên";
            var model = new Employee()
            {
                EmployeeID = 0,
                IsWorking = true
            };
            return View("Edit", model);
        }

   

        /// <summary>
        /// cập nhập nhân viên
        /// </summary>
        /// <param name="id">mã nhân viên cần cập nhập</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin nhân viên";
            var model = await HrDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> SaveData(Employee data, IFormFile? uploadPhoto)
        {
            try
            {
                ViewBag.Title = data.EmployeeID == 0 ? "Bổ sung nhân viên" : "Cập nhật thông tin nhân viên";

                //Kiểm tra dữ liệu đầu vào: FullName và Email là bắt buộc, Email chưa được sử dụng bởi nhân viên khác
                if (string.IsNullOrWhiteSpace(data.FullName))
                    ModelState.AddModelError(nameof(data.FullName), "Vui lòng nhập họ tên nhân viên");
                
                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email nhân viên");
                else if (!await HrDataService.ValidateEmailAsync(data.Email, data.EmployeeID))
                    ModelState.AddModelError(nameof(data.Email), "Email đã được sử dụng bởi nhân viên khác");

                if (!ModelState.IsValid)
                    return View("Edit", data);

                //Xử lý upload ảnh
                if (uploadPhoto != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
                    var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images/employees", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = fileName;
                }

                //Tiền xử lý dữ liệu trước khi lưu vào database
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Photo)) data.Photo = "nophoto.png";

                //Lưu dữ liệu vào database (bổ sung hoặc cập nhật)
                if (data.EmployeeID == 0)
                {
                    await HrDataService.AddEmployeeAsync(data);
                }
                else
                {
                    await HrDataService.UpdateEmployeeAsync(data);
                }
                return RedirectToAction("Index");
            }
            catch //(Exception ex)
            {
                //TODO: Ghi log lỗi căn cứ vào ex.Message và ex.StackTrace
                ModelState.AddModelError(string.Empty, "Hệ thống đang bận hoặc dữ liệu không hợp lệ. Vui lòng kiểm tra dữ liệu hoặc thử lại sau");
                return View("Edit", data);
            }
        }



        /// <summary>
        /// xóa nhân viên
        /// </summary>
        /// <param name="id">mã nhân viên cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await HrDataService.DeleteEmployeeAsync(id);
                return RedirectToAction("Index");
            }



            var model = await HrDataService.GetEmployeeAsync(id);

            if (model == null)
            {
                return RedirectToAction("Index");
            }

            ViewBag.CanDelete = !await HrDataService.isUseAsync(id);

            return View(model);
        }

        /// <summary>
        /// thay đổi mật khẩu nhân viên
        /// </summary>
        /// <param name="id">mã nhân viên cần thay  đổi mật khẩu</param>
        /// <returns></returns>
        public async Task<IActionResult> ChangePassword(int id)
        {
            ViewBag.Title = "Đổi mật khẩu nhân viên";
            var model = await HrDataService.GetEmployeeAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }

        /// <summary>
        /// phân quyền cho nhân viên
        /// </summary>
        /// <param name="id">mã nhân viên cần thay đổi quyền</param>
        /// <returns></returns>
        public async Task<IActionResult> ChangeRole(int id)
        {
            ViewBag.Title = "Phân quyền nhân viên";
            var model = await HrDataService.GetEmployeeAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }
            return View(model);
        }

        /// <summary>
        /// Xử lý lưu phân quyền (POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveRole(int employeeId, string[] roles)
        {
            var model = await HrDataService.GetEmployeeAsync(employeeId);
            if (model == null)
                return RedirectToAction("Index");

            model.RoleNames = (roles != null && roles.Length > 0) ? string.Join(",", roles) : "";

            bool result = await HrDataService.UpdateEmployeeAsync(model);

            if (result)
            {
                // Sử dụng định danh riêng RoleSuccess để tránh hiện nhầm bên trang Password
                TempData["RoleSuccess"] = $"Đã cập nhật quyền cho nhân viên {model.FullName} thành công.";
                return RedirectToAction("ChangeRole", new { id = employeeId });
            }
            else
            {
                ModelState.AddModelError("Error", "Không thể cập nhật phân quyền vào hệ thống.");
                return View("ChangeRole", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePassword(int employeeId, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword)
            {
                ModelState.AddModelError("Error", "Mật khẩu xác nhận không khớp hoặc đang để trống.");
                var emp = await HrDataService.GetEmployeeAsync(employeeId);
                return View("ChangePassword", emp);
            }

            bool result = await HrDataService.ChangePasswordAsync(employeeId, newPassword);

            if (result)
            {
                // Sử dụng định danh riêng PasswordSuccess để tránh hiện nhầm bên trang Role
                TempData["PasswordSuccess"] = "Đã đổi mật khẩu nhân viên thành công.";
                return RedirectToAction("ChangePassword", new { id = employeeId });
            }
            else
            {
                ModelState.AddModelError("Error", "Lỗi hệ thống khi cập nhật mật khẩu vào cơ sở dữ liệu.");
                var emp = await HrDataService.GetEmployeeAsync(employeeId);
                return View("ChangePassword", emp);
            }
        }
    }
}
