using Microsoft.AspNetCore.Mvc;
using SV22T1020494.BusinessLayers;
using SV22T1020494.Models.Catalog;
using SV22T1020494.Models.Common;

namespace SV22T1020494.Shop.Controllers
{
    /// <summary>
    /// Các chức năng cho trang liên quan đến mặt hàng
    /// </summary>
    public class ProductsController : Controller
    {

        /// <summary>
        /// Trang chi tiết mặt hàng (Detail)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Detail(int id)
        {
            try
            {
                var product = await CatalogDataService.GetProductAsync(id);
                if (product == null)
                    return NotFound();

                var categoryInput = new PaginationSearchInput { Page = 1, PageSize = 50 };
                var categoriesResult = await CatalogDataService.ListCategoriesAsync(categoryInput);
                ViewBag.Categories = categoriesResult.DataItems ?? new List<Category>();

                var photos = await CatalogDataService.ListPhotosAsync(id);
                ViewBag.Photos = photos;

                if (product.SupplierID.HasValue && product.SupplierID.Value > 0)
                {
                    try
                    {
                        var sup = await PartnerDataService.GetSupplierAsync(product.SupplierID.Value);
                        ViewBag.SupplierName = sup?.ContactName ?? sup?.SupplierName ?? string.Empty;
                    }
                    catch { ViewBag.SupplierName = string.Empty; }
                }

                try
                {
                    var attrs = await CatalogDataService.ListAttributesAsync(id);
                    ViewBag.Attributes = attrs;
                }
                catch { ViewBag.Attributes = new List<ProductAttribute>(); }

                return View("~/Views/Product/Detail.cshtml", product);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Lấy thông tin mặt hàng cho trang chi tiết
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetProductsByIds(string ids)
        {
            if (string.IsNullOrWhiteSpace(ids))
                return Json(new List<SV22T1020494.Shop.Models.ProductViewModel>());

            var parts = ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var result = new List<SV22T1020494.Shop.Models.ProductViewModel>();
            foreach (var p in parts)
            {
                if (int.TryParse(p, out var id))
                {
                    var prod = await CatalogDataService.GetProductAsync(id);
                    if (prod != null)
                    {
                        result.Add(new SV22T1020494.Shop.Models.ProductViewModel {
                            ProductID = prod.ProductID,
                            ProductName = prod.ProductName,
                            Price = prod.Price,
                            Photo = prod.Photo,
                            IsSelling = prod.IsSelling
                        });
                    }
                }
            }

            return Json(result);
        }
    }
}
