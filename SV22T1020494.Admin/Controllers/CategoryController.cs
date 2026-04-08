using Microsoft.AspNetCore.Mvc;
using SV22T1020494.BusinessLayers;
using SV22T1020494.Models;
using SV22T1020494.Models.Common;
using SV22T1020494.Models.Catalog;
using System.Threading.Tasks;

namespace SV22T1020494.Admin.Controllers
{
    public class CategoryController : Controller
    {
        private const int PAGE_SIZE = 10;
        private const string CATEGORY_SEARCH_INPUT = "CategorySearchInput";

        /// <summary>
        /// Hiển thị danh sách loại hàng, hỗ trợ phân trang và tìm kiếm theo tên hoặc mô tả.
        /// </summary>
        public async Task<IActionResult> Index(int page = 1, string searchValue = "")
        {
            ViewBag.Title = "Quản lý loại hàng";
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CATEGORY_SEARCH_INPUT);
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

            var result = await CatalogDataService.ListCategoriesAsync(input);

            ViewBag.SearchValue = input.SearchValue;
            ViewBag.Page = input.Page;
            ViewBag.PageSize = input.PageSize;
            ViewBag.TotalRows = result.RowCount;
            ViewBag.PageCount = result.PageCount;

            return View(input);
        }

        /// <summary>
        /// Xóa loại hàng theo mã và thông báo kết quả.
        /// </summary>
        [HttpPost]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var success = await CatalogDataService.DeleteCategoryAsync(id);
            if (success) TempData["Message"] = "Đã xóa loại hàng";
            else TempData["Error"] = "Không thể xóa loại hàng vì có dữ liệu liên quan";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            ViewBag.Title = "Xóa loại hàng";
            var cat = await CatalogDataService.GetCategoryAsync(id);
            if (cat == null) return RedirectToAction("Index");
            return View(cat);
        }

        /// <summary>
        /// Hiển thị form thêm loại hàng mới (tái sử dụng view Edit).
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung loại hàng";
            return View("Edit", new SV22T1020494.Models.CategoryViewModel { CategoryID = 0 });
        }

        /// <summary>
        /// Hiển thị form cập nhật thông tin loại hàng.
        /// </summary>
        /// <param name="id">Mã loại hàng cần cập nhật</param>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật loại hàng";
            var cat = await CatalogDataService.GetCategoryAsync(id);
            if (cat == null) return RedirectToAction("Index");

            var model = new SV22T1020494.Models.CategoryViewModel
            {
                CategoryID = cat.CategoryID,
                CategoryName = cat.CategoryName,
                Description = cat.Description
            };

            return View(model);
        }

        /// <summary>
        /// Lưu thông tin loại hàng (dùng chung cho thêm mới và cập nhật).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(SV22T1020494.Models.CategoryViewModel model)
        {
            // Kiểm tra dữ liệu đầu vào
            if (!ModelState.IsValid)
            {
                ViewBag.Title = model.CategoryID == 0 ? "Bổ sung loại hàng" : "Cập nhật loại hàng";
                return View("Edit", model);
            }

            var domain = new Category
            {
                CategoryID = model.CategoryID,
                CategoryName = model.CategoryName,
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description
            };

            if (model.CategoryID == 0)
            {
                await CatalogDataService.AddCategoryAsync(domain);
                TempData["Message"] = "Bổ sung loại hàng thành công!";
            }
            else
            {
                await CatalogDataService.UpdateCategoryAsync(domain);
                TempData["Message"] = "Cập nhật loại hàng thành công!";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Tìm kiếm loại hàng theo tiêu chí và phân trang.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Search(int page = 1, int pageSize = PAGE_SIZE, string searchValue = "")
        {
            var input = new PaginationSearchInput { Page = page, PageSize = pageSize, SearchValue = searchValue };
            var result = await CatalogDataService.ListCategoriesAsync(input);
            ApplicationContext.SetSessionData(CATEGORY_SEARCH_INPUT, input);
            return View(result);
        }
    }
}