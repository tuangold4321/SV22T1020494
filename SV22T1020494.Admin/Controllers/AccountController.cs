using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020494.Admin;
using SV22T1020494.Admin.Models;
using SV22T1020494.Models;
using SV22T1020494.Models.Sales;
using SV22T1020494.Models.Security;
using System.Threading.Tasks;
using SV22T1020494.BusinessLayers;
using System.Linq;

namespace SV22T1020494.Admin.Controllers
{
    /// <summary>
    /// Quản lý các chức năng liên quan đến tài khoản: đăng nhập và đổi mật khẩu.
    /// </summary>
    [Authorize]
    public class AccountController : Controller
    {
        /// <summary>
        /// Hiển thị trang đăng nhập.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// Xử lý đăng nhập (POST).
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string username, string password)
        {
            ViewBag.UserName = username;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Error", "Nhập đầy đủ Email và mật khẩu!");
                return View();
            }
            password = CryptHelper.HashMD5(password);

            var authResult = await SecurityDataService.EmployeeAuthenticateAsync(username, password);

            if (authResult.Status != SV22T1020494.Models.Security.AuthenticationStatus.Success)
            {
                switch (authResult.Status)
                {
                    case SV22T1020494.Models.Security.AuthenticationStatus.Locked:
                        ModelState.AddModelError("Error", "Không thể đăng nhập: Nhân viên này đã ngưng làm việc.");
                        break;
                    case SV22T1020494.Models.Security.AuthenticationStatus.InvalidPassword:
                    default:
                        ModelState.AddModelError("Error", "Email hoặc mật khẩu không đúng!");
                        break;
                }
                return View();
            }

            var userAccount = authResult.User!;

            var userData = new WebUserData()
            {
                UserId = userAccount.UserId,
                UserName = userAccount.UserName,
                DisplayName = userAccount.DisplayName,
                Email = userAccount.Email,
                Photo = userAccount.Photo,
                Roles = (userAccount.RoleNames ?? string.Empty)
                            .Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries)
                            .Select(r => r.Trim())
                            .Where(r => !string.IsNullOrEmpty(r))
                            .ToList()
            };

            await HttpContext.SignInAsync
            (
                CookieAuthenticationDefaults.AuthenticationScheme,
                userData.CreatePrincipal()
            );

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Đăng xuất
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Hiển thị trang đổi mật khẩu.
        /// </summary>
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        /// <summary>
        /// Xử lý yêu cầu đổi mật khẩu (POST).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userData = User.GetUserData();
            if (userData == null || string.IsNullOrWhiteSpace(userData.UserName))
                return RedirectToAction("Login");

            var oldHashed = CryptHelper.HashMD5(model.OldPassword);
            var authorized = await SecurityDataService.EmployeeAuthorizeAsync(userData.UserName, oldHashed);
            if (authorized == null)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu cũ không đúng!");
                return View(model);
            }

            var newHashed = CryptHelper.HashMD5(model.NewPassword);
            var success = await SecurityDataService.ChangePasswordAsync(userData.UserName, newHashed);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Không thể cập nhật mật khẩu. Vui lòng thử lại sau.");
                return View(model);
            }
            TempData["Message"] = "Đổi mật khẩu thành công!";
            return View(model);
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
