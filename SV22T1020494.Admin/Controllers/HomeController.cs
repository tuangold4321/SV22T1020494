using System.Diagnostics;
using SV22T1020494.Admin;
using SV22T1020494.Admin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace SV22T1020494.Admin.Controllers
{
    /// <summary>
    /// Các Controller, Action dự kiến cho các chức năngCác Controller, Action dự kiến cho các chức năng
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        /// <summary>
        /// Trang chủ hiển thị dashboard/links chính.
        /// </summary>
        /// <returns></returns>

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
