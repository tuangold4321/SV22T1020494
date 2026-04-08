using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SV22T1020494.BusinessLayers;
using SV22T1020494.Models;
using SV22T1020494.Models.Common;
using SV22T1020494.Models.Catalog;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SV22T1020494.Admin.Controllers
{
    public class ProductController : Controller
    {
        private const int PAGE_SIZE = 10;
        private const string PRODUCT_SEARCH_INPUT = "ProductSearchInput";
        private readonly IWebHostEnvironment _env;

        public ProductController(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// Hiển thị danh sách mặt hàng với khả năng lọc theo loại, nhà cung cấp, khoảng giá và tìm kiếm theo tên.
        /// </summary>
        public async Task<IActionResult> Index(int page = 1, int category = 0, int supplier = 0, decimal minPrice = 0, decimal maxPrice = 0, string searchValue = "")
        {
            ViewBag.Title = "Quản lý mặt hàng";
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCT_SEARCH_INPUT);
            if (input == null)
            {
                input = new ProductSearchInput
                {
                    Page = page,
                    PageSize = PAGE_SIZE,
                    SearchValue = searchValue,
                    CategoryID = category,
                    SupplierID = supplier,
                    MinPrice = minPrice,
                    MaxPrice = maxPrice
                };
            }
            else
            {
                input.Page = page;
                input.PageSize = PAGE_SIZE;
                if (!string.IsNullOrWhiteSpace(searchValue)) input.SearchValue = searchValue;
                input.CategoryID = category;
                input.SupplierID = supplier;
                input.MinPrice = minPrice;
                input.MaxPrice = maxPrice;
            }

            var result = await CatalogDataService.ListProductsAsync(input);

            ViewBag.SearchValue = input.SearchValue;
            ViewBag.Page = input.Page;
            ViewBag.PageSize = input.PageSize;
            ViewBag.TotalRows = result.RowCount;
            ViewBag.PageCount = result.PageCount;

            return View(input);
        }

        [HttpPost]
        [ActionName("Delete")]
        /// <summary>
        /// Xóa một mặt hàng theo mã và chuyển về trang danh sách.
        /// </summary>
        /// <param name="id">Mã mặt hàng cần xóa</param>
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var success = await CatalogDataService.DeleteProductAsync(id);
            if (success) TempData["Message"] = "Đã xóa mặt hàng thành công";
            else TempData["Error"] = "Không thể xóa mặt hàng vì có dữ liệu liên quan";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            ViewBag.Title = "Xóa mặt hàng";
            var p = await CatalogDataService.GetProductAsync(id);
            if (p == null) return RedirectToAction("Index");

            return View(p);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung mặt hàng";
            var model = new SV22T1020494.Models.Catalog.Product { ProductID = 0, IsSelling = true };
            return View("Edit", model);
        }

        /// <summary>
        /// Hiển thị trang cập nhật ảnh cho một mặt hàng.
        /// </summary>
        /// <param name="id">Mã mặt hàng</param>
        /// <param name="photoId">Mã ảnh cần cập nhật</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> EditPhoto(int id, long photoId = 0)
        {
            ViewBag.ProductID = id;
            if (photoId > 0)
            {
                var photo = await CatalogDataService.GetPhotoAsync(photoId);
                if (photo == null) return RedirectToAction("Edit", new { id });
                ViewBag.Title = "Cập nhật ảnh";
                return View(photo);
            }

            ViewBag.Title = "Thêm ảnh";
            var model = new ProductPhoto { ProductID = id, DisplayOrder = 1 };
            return View(model);
        }

        /// <summary>
        /// Lưu ảnh (thêm hoặc cập nhật)
        /// </summary>
        /// <param name="model"></param>
        /// <param name="PhotoFile"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPhoto(ProductPhoto model, IFormFile? PhotoFile)
        {
            if (Request?.Form != null && Request.Form.ContainsKey("IsHidden"))
            {
                var vals = Request.Form["IsHidden"];
                if (vals.Count > 0)
                {
                    var last = vals[vals.Count - 1]?.ToString()?.ToLowerInvariant();
                    model.IsHidden = (last == "true" || last == "1" || last == "on");
                }
            }

            if (model.PhotoID == 0 && (PhotoFile == null || PhotoFile.Length == 0))
            {
                ViewBag.ProductID = model.ProductID;
                ViewBag.Title = model.PhotoID == 0 ? "Thêm ảnh" : "Cập nhật ảnh";
                ModelState.AddModelError(nameof(PhotoFile), "Vui lòng chọn ảnh!");
                return View(model);
            }

            if (PhotoFile != null && PhotoFile.Length > 0)
            {
                var uploads = Path.Combine(_env.WebRootPath ?? "wwwroot", "images", "products");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
                var originalFileName = Path.GetFileName(PhotoFile.FileName);
                var filePath = Path.Combine(uploads, originalFileName);
                if (!System.IO.File.Exists(filePath))
                {
                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        await PhotoFile.CopyToAsync(fs);
                    }
                }
                model.Photo = originalFileName;
            }

            if (model.PhotoID == 0)
            {
                var existing = await CatalogDataService.ListPhotosAsync(model.ProductID);
                var exists = existing != null && existing.Any(p => string.Equals(p.Photo, model.Photo, System.StringComparison.OrdinalIgnoreCase));
                if (!exists)
                {
                    await CatalogDataService.AddPhotoAsync(model);
                    TempData["Message"] = "Đã thêm ảnh";
                }
                else
                {
                    TempData["Message"] = "Ảnh đã tồn tại";
                }
            }
            else
            {
                await CatalogDataService.UpdatePhotoAsync(model);
                TempData["Message"] = "Đã cập nhật ảnh";
            }

            return RedirectToAction("Edit", new { id = model.ProductID });
        }

        /// <summary>
        /// Xóa một ảnh của mặt hàng rồi chuyển về trang chỉnh sửa mặt hàng.
        /// </summary>
        /// <param name="id">Mã mặt hàng có ảnh cần xoá</param>
        /// <param name="PhotoId">Mã ảnh cần xoá</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> DeletePhoto(int id, long PhotoId)
        {
            var success = await CatalogDataService.DeletePhotoAsync(PhotoId);
            if (success) TempData["Message"] = "Đã xóa ảnh";
            else TempData["Error"] = "Không thể xóa ảnh";
            return RedirectToAction("Edit", new { id });
        }

        /// <summary>
        /// Hiển thị form chỉnh sửa thông tin mặt hàng.
        /// </summary>
        /// <param name="id">Mã mặt hàng cần chỉnh sửa</param>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật mặt hàng";
            var p = await CatalogDataService.GetProductAsync(id);
            if (p == null) return RedirectToAction("Index");
            var photos = await CatalogDataService.ListPhotosAsync(id);
            ViewBag.Photos = photos;
            var attrs = await CatalogDataService.ListAttributesAsync(id);
            ViewBag.Attributes = attrs;
            ViewBag.ProductID = id;
            return View(p);
        }

        /// <summary>
        /// Lưu thông tin mặt hàng (Thêm mới hoặc cập nhật)
        /// </summary>
        /// <param name="model">Đối tượng mặt hàng cần lưu</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveData(SV22T1020494.Models.Catalog.Product model, IFormFile? PhotoFile)
        {
            var domain = model;

            if (!ModelState.IsValid)
            {
                ViewBag.Title = model.ProductID == 0 ? "Bổ sung mặt hàng" : "Cập nhật mặt hàng";
                var photosOnInvalid = await CatalogDataService.ListPhotosAsync(domain.ProductID);
                ViewBag.Photos = photosOnInvalid;
                return View("Edit", domain);
            }

            string? uploadedFileName = null;
            if (PhotoFile != null && PhotoFile.Length > 0)
            {
                var uploads = Path.Combine(_env.WebRootPath ?? "wwwroot", "images", "products");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
                var originalFileName = Path.GetFileName(PhotoFile.FileName);
                var filePath = Path.Combine(uploads, originalFileName);
                if (!System.IO.File.Exists(filePath))
                {
                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        await PhotoFile.CopyToAsync(fs);
                    }
                }
                uploadedFileName = originalFileName;
                domain.Photo = uploadedFileName;
            }

            int productId;
            if (model.ProductID == 0)
            {
                productId = await CatalogDataService.AddProductAsync(domain);
                domain.ProductID = productId;
                TempData["Message"] = "Thêm mặt hàng thành công!";
            }
            else
            {
                await CatalogDataService.UpdateProductAsync(domain);
                productId = model.ProductID;
                TempData["Message"] = "Cập nhật mặt hàng thành công!";
            }

            return RedirectToAction("Edit", new { id = productId });
        }

        [HttpGet]
        public async Task<IActionResult> Search(int page = 1, int pageSize = PAGE_SIZE, int category = 0, int supplier = 0, decimal minPrice = 0, decimal maxPrice = 0, string searchValue = "")
        {
            var input = new ProductSearchInput
            {
                Page = page,
                PageSize = pageSize,
                SearchValue = searchValue,
                CategoryID = category,
                SupplierID = supplier,
                MinPrice = minPrice,
                MaxPrice = maxPrice
            };

            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(PRODUCT_SEARCH_INPUT, input);
            return View(result);
        }

        /// <summary>
        /// Hiển thị danh sách thuộc tính của một mặt hàng.
        /// </summary>
        /// <param name="id">Mã mặt hàng</param>
        [HttpGet]
        public async Task<IActionResult> ListAttributes(int id)
        {
            ViewBag.Title = "Thuộc tính mặt hàng";
            ViewBag.ProductID = id;
            var attrs = await CatalogDataService.ListAttributesAsync(id);
            return View(attrs);
        }

        /// <summary>
        /// Return a confirmation partial view to delete an attribute (loaded into modal)
        /// Mapped to action name DeleteAttribute for GET so /Product/DeleteAttribute loads this partial.
        /// </summary>
        [HttpGet]
        [ActionName("DeleteAttribute")]
        public async Task<IActionResult> ConfirmDeleteAttribute(int id, long attributeId)
        {
            ViewBag.ProductID = id;
            var attr = await CatalogDataService.GetAttributeAsync(attributeId);
            if (attr == null) return NotFound();
            return PartialView("DeleteAttribute", attr);
        }

        /// <summary>
        /// Hiển thị form thêm thuộc tính cho một mặt hàng.
        /// </summary>
        /// <param name="id">Mã mặt hàng</param>
        [HttpGet]
        public async Task<IActionResult> AddAttribute(int id)
        {
            ViewBag.Title = "Thêm thuộc tính";
            ViewBag.ProductID = id;
            var attributes = await CatalogDataService.ListAttributesAsync(id);
            ViewBag.Attributes = attributes;
            var model = new ProductAttribute { ProductID = id, DisplayOrder = attributes != null && attributes.Count > 0 ? attributes.Max(a => a.DisplayOrder) + 1 : 1 };
            return View("EditAttribute", model);
        }

        /// <summary>
        /// Hiển thị form cập nhật một thuộc tính của mặt hàng.
        /// </summary>
        /// <param name="id">Mã mặt hàng</param>
        /// <param name="attributeId">Mã thuộc tính cần sửa</param>
        [HttpGet]
        public async Task<IActionResult> EditAttribute(int id, long attributeId)
        {
            ViewBag.Title = "Cập nhật thuộc tính";
            ViewBag.ProductID = id;
            var attributes = await CatalogDataService.ListAttributesAsync(id);
            ViewBag.Attributes = attributes;
            var model = await CatalogDataService.GetAttributeAsync(attributeId);
            if (model == null || model.ProductID != id) return RedirectToAction("Edit", new { id });
            return View(model);
        }

        /// <summary>
        /// Xóa một thuộc tính của mặt hàng và chuyển về danh sách thuộc tính.
        /// </summary>
        /// <param name="id">Mã mặt hàng</param>
        /// <param name="attributeId">Mã thuộc tính cần xóa</param>
        [HttpPost]
        public async Task<IActionResult> DeleteAttribute(int id, long attributeId)
        {
            var success = await CatalogDataService.DeleteAttributeAsync(attributeId);
            if (success) TempData["Message"] = "Đã xóa thuộc tính";
            else TempData["Error"] = "Không thể xóa thuộc tính";
            return RedirectToAction("Edit", new { id });
        }

        /// <summary>
        /// Lưu thuộc tính (thêm hoặc cập nhật)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAttribute(ProductAttribute model)
        {
            if (model.ProductID <= 0)
            {
                ModelState.AddModelError("ProductID", "Mã mặt hàng không hợp lệ.");
            }

            var product = model.ProductID > 0 ? await CatalogDataService.GetProductAsync(model.ProductID) : null;
            if (product == null)
            {
                ModelState.AddModelError("ProductID", "Sản phẩm không tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ProductID = model.ProductID;
                ViewBag.Title = model.AttributeID == 0 ? "Thêm thuộc tính" : "Cập nhật thuộc tính";
                ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(model.ProductID);
                return View("EditAttribute", model);
            }

            if (model.AttributeID == 0)
            {
                await CatalogDataService.AddAttributeAsync(model);
                TempData["Message"] = "Đã thêm thuộc tính";
            }
            else
            {
                await CatalogDataService.UpdateAttributeAsync(model);
                TempData["Message"] = "Đã cập nhật thuộc tính";
            }
            return RedirectToAction("Edit", new { id = model.ProductID });
        }

        /// <summary>
        /// Hiển thị danh sách ảnh của một mặt hàng.
        /// </summary>
        /// <param name="id">Mã mặt hàng</param>
        [HttpGet]
        public async Task<IActionResult> ListPhotos(int id)
        {
            ViewBag.Title = "Ảnh mặt hàng";
            ViewBag.ProductID = id;
            var photos = await CatalogDataService.ListPhotosAsync(id);
            return View(photos);
        }

        /// <summary>
        /// Return a confirmation partial view to delete a photo (loaded into modal)
        /// Mapped to action name DeletePhoto for GET so /Product/DeletePhoto loads this partial.
        /// </summary>
        [HttpGet]
        [ActionName("DeletePhoto")]
        public async Task<IActionResult> ConfirmDeletePhoto(int id, long photoId)
        {
            ViewBag.ProductID = id;
            var photo = await CatalogDataService.GetPhotoAsync(photoId);
            if (photo == null) return NotFound();
            return PartialView("DeletePhoto", photo);
        }

        /// <summary>
        /// Hiển thị form thêm ảnh cho một mặt hàng.
        /// </summary>
        /// <param name="id">Mã mặt hàng</param>
        [HttpGet]
        public IActionResult CreatePhoto(int id)
        {
            ViewBag.Title = "Thêm ảnh";
            ViewBag.ProductID = id;
            return View("EditPhoto");
        }
    }
}