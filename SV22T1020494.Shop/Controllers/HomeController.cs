using Microsoft.AspNetCore.Mvc;
using SV22T1020494.BusinessLayers;
using SV22T1020494.Models.Catalog;
using SV22T1020494.Models.Common;
using SV22T1020494.Shop.Models;
using System.Diagnostics;

namespace SV22T1020494.Shop.Controllers
{
    /// <summary>
    /// Các chức năng xử lý trang chủ, tìm kiếm và các trang thông tin
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        /// <summary>
        /// Trang chủ
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<IActionResult> Index(int page = 1)
        {
            try {
                if (page == 1 && Request.Query.ContainsKey("page"))
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            catch { }
            try
            {
                var categoryInput = new PaginationSearchInput { Page = 1, PageSize = 10 };
                var categoriesResult = await CatalogDataService.ListCategoriesAsync(categoryInput);
                ViewBag.Categories = categoriesResult.DataItems ?? new List<Category>();

                var pageSize = 12;
                var productInput = new ProductSearchInput { Page = page, PageSize = pageSize };
                var productsResult = await CatalogDataService.ListProductsAsync(productInput);
                var products = productsResult.DataItems ?? new List<Product>();

                ViewBag.Products = products;
                ViewBag.TotalProducts = productsResult.RowCount;
                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products and categories");
                ViewBag.Categories = new List<Category>();
                ViewBag.Products = new List<Product>();
                ViewBag.TotalProducts = 0;
            }

            return View();
        }

        /// <summary>
        /// Chức năng tìm kiếm và lọc theo danh mục, nhà cung cấp, giá
        /// </summary>
        /// <param name="q"></param>
        /// <param name="categories"></param>
        /// <param name="supplier"></param>
        /// <param name="sort"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<IActionResult> Search(string q, int? categories, int? supplier, string sort, int page = 1)
        {
            try
            {
                var categoryInput = new PaginationSearchInput { Page = 1, PageSize = 50 };
                var categoriesResult = await CatalogDataService.ListCategoriesAsync(categoryInput);
                ViewBag.Categories = categoriesResult.DataItems ?? new List<Category>();

                var supplierInput = new PaginationSearchInput { Page = 1, PageSize = 50 };
                var suppliersResult = await PartnerDataService.ListSuppliersAsync(supplierInput);
                ViewBag.Suppliers = suppliersResult.DataItems ?? new List<SV22T1020494.Models.Partner.Supplier>();

                var pageSize = 12;
                var productInput = new ProductSearchInput
                {
                    Page = 1,
                    PageSize = 0, 
                    SearchValue = q,
                    CategoryID = 0,
                    SupplierID = 0
                };

                var productsResult = await CatalogDataService.ListProductsAsync(productInput);
                var allProducts = (productsResult.DataItems ?? new List<Product>()).Where(p => p.IsSelling).ToList();

                if (categories.HasValue && categories.Value > 0)
                {
                    allProducts = allProducts.Where(p => p.CategoryID.HasValue && p.CategoryID.Value == categories.Value).ToList();
                }

                if (supplier.HasValue && supplier.Value > 0)
                {
                    allProducts = allProducts.Where(p => p.SupplierID.HasValue && p.SupplierID.Value == supplier.Value).ToList();
                }

                if (!string.IsNullOrWhiteSpace(sort))
                {
                    if (sort == "price_desc")
                        allProducts = allProducts.OrderByDescending(p => p.Price).ToList();
                    else if (sort == "price_asc")
                        allProducts = allProducts.OrderBy(p => p.Price).ToList();
                }

                var total = allProducts.Count;

                var paged = allProducts.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                ViewBag.Products = paged;
                ViewBag.Query = q ?? string.Empty;
                ViewBag.Sort = sort ?? string.Empty;
                ViewBag.SelectedCategories = categories.HasValue && categories.Value > 0 ? new int[] { categories.Value } : new int[0];
                ViewBag.TotalProducts = total;
                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.SelectedSupplier = supplier ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products");
                ViewBag.Categories = new List<Category>();
                ViewBag.Products = new List<Product>();
                ViewBag.Query = q ?? string.Empty;
                ViewBag.Sort = sort ?? string.Empty;
                ViewBag.SelectedCategories = new int[0];
            }

            return View();
        }

        /// <summary>
        /// Báo lỗi nếu không tìm thấy
        /// </summary>
        /// <returns></returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
