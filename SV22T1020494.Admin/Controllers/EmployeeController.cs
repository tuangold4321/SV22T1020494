using SV22T1020494.BusinessLayers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SV22T1020494.Models;
using SV22T1020494.Models.Common;
using SV22T1020494.Models.HR;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SV22T1020494.Admin.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private const int PAGE_SIZE = 10;
        private const string EMPLOYEE_SEARCH_INPUT = "EmployeeSearchInput";

        public EmployeeController(IWebHostEnvironment env)
        {
            _env = env;
        }
        /// <summary>
        /// Trang nhân viên
        /// </summary>
        /// <param name="page"></param>
        /// <param name="searchValue"></param>
        /// <returns></returns>
        public async Task<IActionResult> Index(int page = 1, string searchValue = "")
        {
            ViewBag.Title = "Quản lý nhân viên";
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(EMPLOYEE_SEARCH_INPUT);
            if (input == null)
            {
                input = new PaginationSearchInput { Page = page, PageSize = PAGE_SIZE, SearchValue = searchValue };
            }
            else
            {
                input.Page = page;
                input.PageSize = PAGE_SIZE;
                if (!string.IsNullOrWhiteSpace(searchValue)) input.SearchValue = searchValue;
            }

            var result = await HRDataService.ListEmployeesAsync(input);

            ViewBag.SearchValue = input.SearchValue;
            ViewBag.Page = input.Page;
            ViewBag.PageSize = input.PageSize;
            ViewBag.TotalRows = result.RowCount;
            ViewBag.PageCount = result.PageCount;

            return View(input);
        }
        /// <summary>
        /// Xác nhận xoá nhân viên
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var success = await HRDataService.DeleteEmployeeAsync(id);
            if (success) TempData["Message"] = "Đã xóa nhân viên.";
            else TempData["Error"] = "Không thể xóa nhân viên vì có dữ liệu liên quan.";
            return RedirectToAction("Index");
        }
        /// <summary>
        /// Xoá nhân viên
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            ViewBag.Title = "Xóa nhân viên";
            var emp = await HRDataService.GetEmployeeAsync(id);
            if (emp == null) return RedirectToAction("Index");
            return View(emp);
        }
        /// <summary>
        /// Thêm nhân viên mới
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Title = "Thêm nhân viên mới";
            return View("Edit", new EmployeeViewModel { EmployeeID = 0, IsActive = true, BirthDate = null });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật nhân viên";
            var emp = await HRDataService.GetEmployeeAsync(id);
            if (emp == null) return RedirectToAction("Index");
            var names = (emp.FullName ?? string.Empty).Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            string last = names.Length > 0 ? names[0] : string.Empty;
            string first = names.Length > 1 ? names[1] : string.Empty;

            var model = new EmployeeViewModel
            {
                EmployeeID = emp.EmployeeID,
                LastName = last,
                FirstName = first,
                Email = emp.Email,
                Phone = emp.Phone ?? string.Empty,
                Address = emp.Address ?? string.Empty,
                PhotoPath = emp.Photo ?? string.Empty,
                IsActive = emp.IsWorking == true,
                BirthDate = emp.BirthDate
            };

            return View(model);
        }
        /// <summary>
        /// Lưu thay đổi
        /// </summary>
        /// <param name="model"></param>
        /// <param name="avatar"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(EmployeeViewModel model, IFormFile? avatar)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Title = model.EmployeeID == 0 ? "Thêm nhân viên mới" : "Cập nhật nhân viên";
                return View("Edit", model);
            }

            if (avatar != null && avatar.Length > 0)
            {
                var uploads = Path.Combine(_env.WebRootPath ?? "wwwroot", "images", "employees");
                Directory.CreateDirectory(uploads);
                var fileName = $"emp_{Guid.NewGuid()}{Path.GetExtension(avatar.FileName)}";
                var filePath = Path.Combine(uploads, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatar.CopyToAsync(stream);
                }
                model.PhotoPath = $"/images/employees/{fileName}";
            }
            var domain = new Employee
            {
                EmployeeID = model.EmployeeID,
                FullName = $"{model.LastName} {model.FirstName}",
                Email = model.Email,
                Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone,
                Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address,
                Photo = string.IsNullOrWhiteSpace(model.PhotoPath) ? null : model.PhotoPath,
                BirthDate = model.BirthDate,
                IsWorking = model.IsActive
            };

            if (model.EmployeeID == 0)
            {
                await HRDataService.AddEmployeeAsync(domain);
                TempData["Message"] = "Thêm nhân viên thành công!";
            }
            else
            {
                await HRDataService.UpdateEmployeeAsync(domain);
                TempData["Message"] = "Cập nhật thông tin thành công!";
            }

            return RedirectToAction("Index");
        }
        /// <summary>
        /// Tìm kiếm nhân viên
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <param name="searchValue"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Search(int page = 1, int pageSize = PAGE_SIZE, string searchValue = "")
        {
            var input = new PaginationSearchInput { Page = page, PageSize = pageSize, SearchValue = searchValue };
            var result = await HRDataService.ListEmployeesAsync(input);
            ApplicationContext.SetSessionData(EMPLOYEE_SEARCH_INPUT, input);
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            ViewBag.Title = "Đổi mật khẩu";
            var emp = await HRDataService.GetEmployeeAsync(id);
            if (emp == null) return RedirectToAction("Index");
            return View(emp);
        }
        /// <summary>
        /// Đổi mật khẩu nhân viên
        /// </summary>
        /// <param name="EmployeeID"></param>
        /// <param name="NewPassword"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(int EmployeeID, string NewPassword)
        {
            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                TempData["Error"] = "Mật khẩu mới không được để trống.";
                return RedirectToAction("ChangePassword", new { id = EmployeeID });
            }

            var emp = await HRDataService.GetEmployeeAsync(EmployeeID);
            if (emp == null)
            {
                TempData["Error"] = "Không tìm thấy nhân viên.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(emp.Email))
            {
                TempData["Error"] = "Nhân viên chưa có email. Không thể đổi mật khẩu.";
                return RedirectToAction("Index");
            }

            var hashed = CryptHelper.HashMD5(NewPassword);

            var success = await SecurityDataService.ChangePasswordAsync(emp.Email, hashed);
            if (success)
            {
                TempData["Message"] = "Đổi mật khẩu thành công!";
            }
            else
            {
                TempData["Error"] = "Không thể cập nhật mật khẩu. Vui lòng thử lại.";
            }

            return RedirectToAction("Index");
        }
        /// <summary>
        /// Sửa vai trò nhân viên
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> ChangeRole(int id)
        {
            ViewBag.Title = "Phân quyền";
            var emp = await HRDataService.GetEmployeeAsync(id);
            if (emp == null) return RedirectToAction("Index");
            return View(emp);
        }
        /// <summary>
        /// Cập nhật vai trò
        /// </summary>
        /// <param name="EmployeeID"></param>
        /// <param name="Roles"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(int EmployeeID, string[]? Roles)
        {
            var emp = await HRDataService.GetEmployeeAsync(EmployeeID);
            if (emp == null)
            {
                TempData["Error"] = "Không tìm thấy nhân viên.";
                return RedirectToAction("Index");
            }

            emp.RoleNames = Roles == null ? string.Empty : string.Join(',', Roles);

            var success = await HRDataService.UpdateEmployeeAsync(emp);
            if (success)
            {
                TempData["Message"] = "Cập nhật quyền thành công!";
            }
            else
            {
                TempData["Error"] = "Không thể cập nhật quyền. Vui lòng thử lại.";
            }

            return RedirectToAction("Index");
        }
    }
}