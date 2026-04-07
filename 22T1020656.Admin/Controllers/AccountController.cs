using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using SV22T1020656.BusinessLayers;
using SV22T1020656.Models.Security;
using System.Reflection;
using System.Threading.Tasks;

namespace SV22T1020656.Admin.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        /// <summary>
        /// đăng nhập tài khoản của người dùng
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]        
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(String username, String password)
        {
            ViewBag.Username = username;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập Email và Mật khẩu");
                return View();
            }

          
            var userAccount = await SecurityDataService.LoginAsync(username, CryptHelper.HashMD5(password));

            if (userAccount != null)
            {
               
                var userData = new WebUserData()
                {
                    UserId = userAccount.UserId,
                    UserName = userAccount.UserName,
                    DisplayName = userAccount.DisplayName,
                    Email = userAccount.Email,
                    Photo = userAccount.Photo,
                    
                    Roles = userAccount.RoleNames?.Split(',').Select(r => r.Trim()).ToList() ?? new List<string>()
                };

                
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    userData.CreatePrincipal(),
                    new AuthenticationProperties { IsPersistent = true }
                );

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("Error", "Tên đăng nhập hoặc mật khẩu không chính xác");
            return View();
        }

        /// <summary>
        /// thực hiện chức năng đăng xuất của người dùng
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(registerModel model)
        {


            if (!ModelState.IsValid)
                return View(model);

            //  Kiểm tra khớp mật khẩu TRƯỚC KHI HASH
            if (model.Password != model.RePassword)
            {
                ModelState.AddModelError("RePassword", "Xác nhận mật khẩu không khớp.");
                return View(model);
            }

            model.Password = CryptHelper.HashMD5(model.Password);
            var result = await SecurityDataService.RegisterEmployee(model);

            if (result)
            {
                TempData["Success"] = "Đăng ký tài khoản thành công!";
                return RedirectToAction("Login");
            }
            else
            {
               
                ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản khác.");
                return View(model);
            }

            //return RedirectToAction("Login");

        }

        /// <summary>
        /// thực hiện chức năng thay đổi mật khẩu của người dùng 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin.");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Xác nhận mật khẩu mới không khớp.");
                return View();
            }

           
            string userName = User.Identity?.Name;
            if (string.IsNullOrEmpty(userName))
                return RedirectToAction("Login");

            
            string hashedOldPassword = CryptHelper.HashMD5(oldPassword);
            var user = await SecurityDataService.LoginAsync(userName, hashedOldPassword);

            if (user == null)
            {
                ModelState.AddModelError("", "Mật khẩu hiện tại không chính xác.");
                return View();
            }

           
            string hashedNewPassword = CryptHelper.HashMD5(newPassword);
            bool isUpdated = await SecurityDataService.ChangePasswordAsync(userName, hashedNewPassword);

            if (isUpdated)
            {
                // Gửi thông báo qua TempData vì có Redirect
                TempData["SuccessMessage"] = "Mật khẩu của bạn đã được thay đổi thành công!";
                return RedirectToAction("ChangePassword");
            }

            ModelState.AddModelError("", "Lỗi hệ thống khi cập nhật mật khẩu.");
            return View();
        }

        /// <summary>
        /// Trang báo lỗi khi không có quyền truy cập
        /// </summary>
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
