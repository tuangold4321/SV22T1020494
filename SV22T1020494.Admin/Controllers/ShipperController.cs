using Microsoft.AspNetCore.Mvc;
using SV22T1020494.BusinessLayers;
using SV22T1020494.Models;
using SV22T1020494.Models.Common;
using SV22T1020494.Models.Partner;
using System.Threading.Tasks;

namespace SV22T1020494.Admin.Controllers
{
    public class ShipperController : Controller
    {
        private const int PAGE_SIZE = 10;
        private const string SHIPPER_SEARCH_INPUT = "ShipperSearchInput";

        /// <summary>
        /// Hiển thị danh sách đơn vị vận chuyển, hỗ trợ tìm kiếm theo tên hoặc số điện thoại.
        /// </summary>
        public async Task<IActionResult> Index(int page = 1, string searchValue = "")
        {
            ViewBag.Title = "Quản lý người giao hàng";
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SHIPPER_SEARCH_INPUT);
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

            var result = await PartnerDataService.ListShippersAsync(input);

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
            var success = await PartnerDataService.DeleteShipperAsync(id);
            if (success) TempData["Message"] = "Đã xóa người giao hàng";
            else TempData["Error"] = "Không thể xóa người giao hàng vì có dữ liệu liên quan";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            ViewBag.Title = "Xóa người giao hàng";
            var shipper = await PartnerDataService.GetShipperAsync(id);
            if (shipper == null) return RedirectToAction("Index");
            return View(shipper);
        }

        /// <summary>
        /// Hiển thị form thêm đơn vị vận chuyển mới.
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung người giao hàng";
            return View("Edit", new SV22T1020494.Models.ShipperViewModel { ShipperID = 0 });
        }

        /// <summary>
        /// Hiển thị form cập nhật thông tin đơn vị vận chuyển.
        /// </summary>
        /// <param name="id">Mã đơn vị vận chuyển cần cập nhật</param>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật người giao hàng";
            var shipper = await PartnerDataService.GetShipperAsync(id);
            if (shipper == null) return RedirectToAction("Index");

            var model = new SV22T1020494.Models.ShipperViewModel
            {
                ShipperID = shipper.ShipperID,
                ShipperName = shipper.ShipperName,
                Phone = shipper.Phone ?? string.Empty
            };

            return View(model);
        }

        /// <summary>
        /// Lưu thông tin đơn vị vận chuyển (dùng chung cho thêm và sửa).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(SV22T1020494.Models.ShipperViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Title = model.ShipperID == 0 ? "Bổ sung người giao hàng" : "Cập nhật người giao hàng";
                return View("Edit", model);
            }

            var domain = new Shipper
            {
                ShipperID = model.ShipperID,
                ShipperName = model.ShipperName,
                Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone
            };

            if (model.ShipperID == 0)
            {
                await PartnerDataService.AddShipperAsync(domain);
                TempData["Message"] = "Thêm người giao hàng thành công!";
            }
            else
            {
                await PartnerDataService.UpdateShipperAsync(domain);
                TempData["Message"] = "Cập nhật người giao hàng thành công!";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Tìm kiếm đơn vị vận chuyển theo trang và kích thước trang nhất định.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Search(int page = 1, int pageSize = PAGE_SIZE, string searchValue = "")
        {
            var input = new PaginationSearchInput { Page = page, PageSize = pageSize, SearchValue = searchValue };
            var result = await PartnerDataService.ListShippersAsync(input);
            ApplicationContext.SetSessionData(SHIPPER_SEARCH_INPUT, input);
            return View(result);
        }
    }
}