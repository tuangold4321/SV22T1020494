using Microsoft.AspNetCore.Mvc;
using SV22T1020494.BusinessLayers;
using SV22T1020494.Models;
using SV22T1020494.Models.Common;
using SV22T1020494.Models.Partner;
using System.Linq;
using System.Threading.Tasks;

namespace SV22T1020494.Admin.Controllers
{
    /// <summary>
    /// Quản lý nhà cung cấp: danh sách, thêm, sửa, lưu và xóa.
    /// </summary>
    public class SupplierController : Controller
    {
        private const int PAGE_SIZE = 10;
        private const string SUPPLIER_SEARCH_INPUT = "SupplierSearchInput";

        public async Task<IActionResult> Index(int page = 1, string searchValue = "")
        {
            ViewBag.Title = "Quản lý nhà cung cấp";
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SUPPLIER_SEARCH_INPUT);
            if (input == null)
            {
                input = new PaginationSearchInput
                {
                    Page = page,
                    PageSize = PAGE_SIZE,
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

            var result = await PartnerDataService.ListSuppliersAsync(input);

            ViewBag.SearchValue = input.SearchValue;
            ViewBag.Page = input.Page;
            ViewBag.PageSize = input.PageSize;
            ViewBag.TotalRows = result.RowCount;
            ViewBag.PageCount = result.PageCount;

            return View(input);
        }

        [HttpPost]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var success = await PartnerDataService.DeleteSupplierAsync(id);
            if (success)
                TempData["Message"] = "Đã xóa nhà cung cấp thành công";
            else
                TempData["Error"] = "Không thể xóa nhà cung cấp vì có dữ liệu liên quan";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            ViewBag.Title = "Xóa nhà cung cấp";
            var supplier = await PartnerDataService.GetSupplierAsync(id);
            if (supplier == null) return RedirectToAction("Index");
            return View(supplier);
        }
        /// <summary>
        /// Tạo nhà cung cấp
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhà cung cấp";
            return View("Edit", new SV22T1020494.Models.SupplierViewModel { SupplierID = 0 });
        }
        /// <summary>
        /// Sửa nhà cung cấp
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật nhà cung cấp";
            var supplier = await PartnerDataService.GetSupplierAsync(id);
            if (supplier == null) return RedirectToAction("Index");

            var model = new SV22T1020494.Models.SupplierViewModel
            {
                SupplierID = supplier.SupplierID,
                SupplierName = supplier.SupplierName,
                ContactName = supplier.ContactName,
                Address = supplier.Address ?? string.Empty,
                City = supplier.Province ?? string.Empty,
                Phone = supplier.Phone ?? string.Empty,
                Email = supplier.Email
            };

            return View(model);
        }
        /// <summary>
        /// Lưu nhà cung cấp
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(SV22T1020494.Models.SupplierViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Title = model.SupplierID == 0 ? "Bổ sung nhà cung cấp" : "Cập nhật nhà cung cấp";
                return View("Edit", model);
            }

            var domain = new Supplier
            {
                SupplierID = model.SupplierID,
                SupplierName = model.SupplierName,
                ContactName = model.ContactName ?? string.Empty,
                Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address,
                Province = string.IsNullOrWhiteSpace(model.City) ? null : model.City,
                Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone,
                Email = model.Email
            };

            if (model.SupplierID == 0)
            {
                await PartnerDataService.AddSupplierAsync(domain);
                TempData["Message"] = "Thêm mới nhà cung cấp thành công!";
            }
            else
            {
                await PartnerDataService.UpdateSupplierAsync(domain);
                TempData["Message"] = "Cập nhật nhà cung cấp thành công!";
            }

            return RedirectToAction("Index");
        }
        /// <summary>
        /// Tìm kiếm NCC
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <param name="searchValue"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Search(int page = 1, int pageSize = PAGE_SIZE, string searchValue = "")
        {
            var input = new PaginationSearchInput
            {
                Page = page,
                PageSize = pageSize,
                SearchValue = searchValue
            };

            var result = await PartnerDataService.ListSuppliersAsync(input);
            ApplicationContext.SetSessionData(SUPPLIER_SEARCH_INPUT, input);

            return View(result);
        }
    }
}