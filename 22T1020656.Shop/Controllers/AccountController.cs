using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020656.BusinessLayers;
using SV22T1020656.Models.Partner;
using SV22T1020656.Models.Security;
using System.Security.Claims;




namespace SV22T1020656.Shop.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            string hashedAddress = CryptHelper.HashMD5(password ?? "");
            var user = await SecurityDataService.LoginAsync(username, hashedAddress);

            if (user != null)
            {
                
                var claims = new List<Claim>
        {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim("DisplayName", user.DisplayName),
                    new Claim("UserId", user.UserId),
                    new Claim("Photo", user.Photo ?? "default-user.png"), 
                    new Claim(ClaimTypes.Role, user.RoleNames)
        };

                
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                return RedirectToAction("Index", "Product");
            }

            ModelState.AddModelError("", "Sai tên đăng nhập hoặc mật khẩu");
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(registerModel model)
        {
            
                if (!ModelState.IsValid)
                    return View(model);

                // 2. Kiểm tra khớp mật khẩu TRƯỚC KHI HASH
                if (model.Password != model.RePassword)
                {
                    ModelState.AddModelError("RePassword", "Xác nhận mật khẩu không khớp.");
                    return View(model);
                }

                model.Password = CryptHelper.HashMD5(model.Password);
                var result = await SecurityDataService.RegisterAsync(model);

                if (result)
                {
                    TempData["Success"] = "Đăng ký tài khoản thành công!";
                    return RedirectToAction("Login");
                }
                else
                {
                    // Nếu vào đây, nghĩa là mật khẩu đã khớp nhưng không lưu được 
                    // -> Thường là do trùng Email trong DB
                    ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản khác.");
                    return View(model);
                }

            //return RedirectToAction("Login");
            }
        
        

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            HttpContext.Session.Remove("Cart");

            return RedirectToAction("Index", "Product");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            int customerId = int.Parse(userId);
            var data = await PartnerDataService.GetCustomerAsync(customerId);

            if (data == null)
            {
                return NotFound();
            }


            return View(data);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(Customer data)
        {
            if (string.IsNullOrWhiteSpace(data.CustomerName))
                ModelState.AddModelError(nameof(data.CustomerName), "Tên khách hàng không được để trống");

            if (string.IsNullOrWhiteSpace(data.ContactName))
                ModelState.AddModelError(nameof(data.ContactName), "Tên giao dịch không được để trống");

            // 2. Kiểm tra trùng Email thông qua hàm ValidateAsync đã có trong Service
            bool isEmailValid = await PartnerDataService.ValidateAsync(data.Email, data.CustomerID);
            if (!isEmailValid)
            {
                ModelState.AddModelError(nameof(data.Email), "Email này đã được sử dụng bởi một tài khoản khác");
            }

            if (!ModelState.IsValid)
            {
                return View("Profile", data);
            }

            var currentData = await PartnerDataService.GetCustomerAsync(data.CustomerID);
            if (currentData != null)
            {
                data.Password = currentData.Password; // Giữ nguyên mật khẩu cũ
                data.IsLocked = currentData.IsLocked; // Giữ nguyên trạng thái khóa
            }

            bool result = await PartnerDataService.UpdateCustomerAsync(data);
            if (result)
            {
                TempData["Success"] = "Cập nhật thông tin cá nhân thành công!";
                return RedirectToAction("Profile");
            }
            else
            {
                ModelState.AddModelError("", "Cập nhật dữ liệu thất bại. Vui lòng thử lại");
                return View("Profile", data);
            }
        }

    

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin mật khẩu.");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Xác nhận mật khẩu mới không khớp.");
                return View();
            }

            if (oldPassword == newPassword)
            {
                ModelState.AddModelError("", "Mật khẩu mới không được trùng với mật khẩu cũ.");
                return View();
            }

            string userName = User.Identity.Name;

            // MÃ HÓA MẬT KHẨU CŨ để xác thực
            string hashedOldPassword = CryptHelper.HashMD5(oldPassword);
            var user = await SecurityDataService.LoginAsync(userName, hashedOldPassword);

            if (user == null)
            {
                ModelState.AddModelError("", "Mật khẩu hiện tại không chính xác.");
                return View();
            }

            // MÃ HÓA MẬT KHẨU MỚI để cập nhật vào DB
            string hashedNewPassword = CryptHelper.HashMD5(newPassword);
            bool isUpdated = await SecurityDataService.ChangePasswordAsync(userName, hashedNewPassword);

            if (isUpdated)
            {
                TempData["SuccessMessage"] = "Mật khẩu của bạn đã được thay đổi thành công!";
                return RedirectToAction("ChangePassword");
            }

            ModelState.AddModelError("", "Lỗi hệ thống khi cập nhật mật khẩu.");
            return View();
        }






    }
}
