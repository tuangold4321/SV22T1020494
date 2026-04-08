using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using SV22T1020494.BusinessLayers;
using SV22T1020494.Models;
using SV22T1020494.Models.Common;
using SV22T1020494.Models.Partner;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SV22T1020494.Admin.Controllers
{
    /// <summary>
    /// Quản lý khách hàng: danh sách, thêm, sửa, lưu và xóa.
    /// </summary>
    [Authorize]
    public class CustomerController : Controller
    {
        private const int PAGE_SIZE = 10;
        /// <summary>
        /// Tên biến session lưu điều kiện tìm kiếm khách hàng
        /// </summary>
        private const string CUSTOMER_SEARCH_INPUT = "CustomerSearchInput";

        private readonly ILogger<CustomerController> _logger;

        public CustomerController(ILogger<CustomerController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Hiển thị danh sách khách hàng, và hỗ trợ tìm kiếm theo tên, liên hệ, email hoặc địa chỉ dưới
        /// dạng phân trang.
        /// </summary>
        public async Task<IActionResult> Index(int page = 1, string searchValue = "")
        {
            ViewBag.Title = "Quản lý khách hàng";
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CUSTOMER_SEARCH_INPUT);
            if (input == null)
            {
                input = new PaginationSearchInput
                {
                    Page = page,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = searchValue
                };
            }
            else
            {
                input.Page = page;
                input.PageSize = PAGE_SIZE;
                if (!string.IsNullOrWhiteSpace(searchValue))
                    input.SearchValue = searchValue;
            }

            var result = await PartnerDataService.ListCustomerAsync(input);
            

            ViewBag.SearchValue = searchValue;
            ViewBag.Page = page;
            ViewBag.PageSize = PAGE_SIZE;
            ViewBag.TotalRows = result.RowCount;
            ViewBag.PageCount = result.PageCount;

            return View(input);
        }

        /// <summary>
        /// Xóa khách hàng theo mã.
        /// </summary>
        [HttpPost]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var success = await PartnerDataService.DeleteCustomerAsync(id);
            if (success)
            {
                TempData["Message"] = "Đã xóa khách hàng thành công";
            }
            else
            {
                TempData["Error"] = "Không thể xóa khách hàng vì có dữ liệu liên quan";
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Hiển thị form xác nhận xóa khách hàng.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            ViewBag.Title = "Xóa khách hàng";
            var customer = await PartnerDataService.GetCustomerAsync(id);
            if (customer == null) return RedirectToAction("Index");

            return View(customer);
        }

        /// <summary>
        /// Hiển thị form thêm khách hàng mới (tái sử dụng view Edit).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Title = "Bổ sung khách hàng";
            var provinces = await DictionaryDataService.ListProvinceAsync();
            ViewBag.Provinces = new SelectList(provinces, "ProvinceName", "ProvinceName");

            return View("Edit", new CustomerViewModel { CustomerID = 0, IsLocked = false });
        }

        /// <summary>
        /// Hiển thị form cập nhật thông tin khách hàng.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật khách hàng";
            var customer = await PartnerDataService.GetCustomerAsync(id);
            if (customer == null) return RedirectToAction("Index");

            var model = new CustomerViewModel
            {
                CustomerID = customer.CustomerID,
                CustomerName = customer.CustomerName,
                ContactName = customer.ContactName,
                Address = customer.Address ?? string.Empty,
                Province = customer.Province ?? string.Empty,
                Country = string.Empty,
                Phone = customer.Phone ?? string.Empty,
                Email = customer.Email,
                IsLocked = customer.IsLocked
            };

            var provinces = await DictionaryDataService.ListProvinceAsync();
            ViewBag.Provinces = new SelectList(provinces, "ProvinceName", "ProvinceName", model.Province);

            return View(model);
        }

        /// <summary>
        /// Lưu thông tin khách hàng (dùng chung cho thêm mới và cập nhật).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaveData(CustomerViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.CustomerName))
                    ModelState.AddModelError(nameof(model.CustomerName), "Vui lòng nhập tên của khách hàng");

                if (string.IsNullOrWhiteSpace(model.Email))
                {
                    ModelState.AddModelError(nameof(model.Email), "Vui lòng cho biết email của khách hàng");
                }
                else
                {
                    try
                    {
                        var isValid = await PartnerDataService.ValidatelCustomerEmailAsync(model.Email, model.CustomerID);
                        if (!isValid)
                        {
                            ModelState.AddModelError(nameof(model.Email), "Email này đã có người sử dụng");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError(ex, "Failed to validate customer email uniqueness for {Email}", model.Email);
                        ModelState.AddModelError(string.Empty, "Không kiểm tra được tính duy nhất của email. Vui lòng thử lại sau.");
                    }
                }

                if (string.IsNullOrWhiteSpace(model.Province))
                    ModelState.AddModelError(nameof(model.Province), "Vui lòng chọn tỉnh/thành");
                if (string.IsNullOrEmpty(model.ContactName)) model.ContactName = "";
                if (string.IsNullOrEmpty(model.Phone)) model.Phone = "";
                if (string.IsNullOrEmpty(model.Address)) model.Address = "";
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during validation for customer {CustomerId}", model?.CustomerID);
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi kiểm tra dữ liệu. Vui lòng thử lại sau.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Title = model.CustomerID == 0 ? "Bổ sung khách hàng" : "Cập nhật thông tin khách hàng";
                var provinces = await DictionaryDataService.ListProvinceAsync();
                ViewBag.Provinces = new SelectList(provinces, "ProvinceName", "ProvinceName", model.Province);
                return View("Edit", model);
            }

            var domain = new Customer
            {
                CustomerID = model.CustomerID,
                CustomerName = model.CustomerName,
                ContactName = model.ContactName,
                Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address,
                Province = string.IsNullOrWhiteSpace(model.Province) ? null : model.Province,
                Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone,
                Email = model.Email,
                IsLocked = model.IsLocked
            };
           

            if (model.CustomerID == 0)
            {
                await PartnerDataService.AddCustomerAsync(domain);
                TempData["Message"] = "Thêm mới khách hàng thành công!";
            }
            else
            {
                await PartnerDataService.UpdateCustomerAsync(domain);
                TempData["Message"] = "Cập nhật thông tin khách hàng thành công!";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Tìm kiếm khách hàng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Search(int page = 1, int pageSize = PAGE_SIZE, string searchValue = "")
        {
            var input = new PaginationSearchInput
            {
                Page = page,
                PageSize = pageSize,
                SearchValue = searchValue
            };

            var result = await PartnerDataService.ListCustomerAsync(input);
            ApplicationContext.SetSessionData(CUSTOMER_SEARCH_INPUT, input);

            return View(result);
        }
        /// <summary>
        /// Đổi mật khẩu khách hàng
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            ViewBag.Title = "Mật khẩu khách hàng";
            var customer = await PartnerDataService.GetCustomerAsync(id);
            if (customer == null) return RedirectToAction("Index");

            ViewBag.CustomerName = customer.CustomerName;
            ViewBag.CustomerEmail = customer.Email;
            ViewBag.IsLocked = customer.IsLocked;
            ViewBag.CustomerID = customer.CustomerID;

            return View(new ChangePasswordViewModel());
        }
        /// <summary>
        /// Cập nhật thay đổi
        /// </summary>
        /// <param name="CustomerID"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult UpdatePassword(int CustomerID, ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.CustomerID = CustomerID;
                return View("ChangePassword", model);
            }
            TempData["Message"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Index");
        }
    }
}