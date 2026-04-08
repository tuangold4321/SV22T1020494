using SV22T1020494.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SV22T1020494.BusinessLayers;
using SV22T1020494.Models.Common;
using SV22T1020494.Models.Sales;
using Microsoft.AspNetCore.Authorization;
using SV22T1020494.Models.Catalog;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SV22T1020494.Admin.Controllers
{
    /// <summary>
    /// Quản lý đơn hàng: danh sách, xóa và các hành động xem/khởi tạo.
    /// </summary>
    [Authorize(Roles = WebUserRoles.Sales)]
    public class OrderController : Controller
    {
        private const string SEARCH_PRODUCT = "SearchProductToSale";
        private const string SEARCH_ORDER = "SearchOrderCondition";
        private const string ORDER_DETAIL_PAGE = "OrderDetailPage";

        private static readonly List<OrderViewInfo> _orders = new List<OrderViewInfo>();


        /// <summary>
        /// Giao diện trang chính quản lý đơn hàng
        /// </summary>
        public async Task<IActionResult> Index([FromQuery] OrderSearchInput input)
        {
            ViewBag.Title = "Quản lý đơn hàng";

            var hasQuery = Request.Query != null && Request.Query.Count > 0;

            if (!hasQuery)
            {
                var sessionInput = ApplicationContext.GetSessionData<OrderSearchInput>(SEARCH_ORDER);
                if (sessionInput != null)
                {
                    input = sessionInput;
                }
                else if (input == null)
                {
                    input = new OrderSearchInput()
                    {
                        Page = 1,
                        PageSize = 20,
                        Status = (OrderStatusEnum)0,
                        DateFrom = null,
                        DateTo = null,
                    };
                }
            }
            else
            {
                input ??= new OrderSearchInput() { Page = 1, PageSize = 20, Status = (OrderStatusEnum)0 };
            }

            ApplicationContext.SetSessionData(SEARCH_ORDER, input);
            var data = await SalesDataService.ListOrdersAsync(input);
            ViewBag.SearchResult = data;

            return View(); 
        }

        /// <summary>
        /// Tìm kiếm đơn hàng (được gọi qua AJAX và trả về partial view)
        /// </summary>
        public async Task<IActionResult> Search(OrderSearchInput input)
        {
            ApplicationContext.SetSessionData(SEARCH_ORDER, input);
            var data = await SalesDataService.ListOrdersAsync(input);
            return PartialView(data);
        }

        /// <summary>
        /// Giao diện thực hiện các chức năng để lập đơn hàng mới
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(SEARCH_PRODUCT);
            if (input == null)
            {
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = 3,
                    SearchValue = "",
                    CategoryID = 0,
                    MinPrice = 0,
                    MaxPrice = 0
                };
            }
            ViewBag.ShoppingCart = ShoppingCartHelper.GetShoppingCart();
            var products = await CatalogDataService.ListProductsAsync(input);
            ViewBag.ProductPaged = products;

            var custInput = new PaginationSearchInput() { Page = 1, PageSize = 0, SearchValue = "" };
            var customers = await PartnerDataService.ListCustomersAsync(custInput);
            var customerSelect = new List<SelectListItem>() { new SelectListItem { Value = "", Text = "-- Chọn khách hàng --" } };
            foreach (var c in customers.DataItems)
            {
                customerSelect.Add(new SelectListItem { Value = c.CustomerID.ToString(), Text = c.CustomerName });
            }
            ViewBag.Customers = customerSelect;

            var provinces = await SelectListHelper.Provinces();
            if (provinces == null || provinces.Count == 0)
            {
                provinces = new List<SelectListItem> { new SelectListItem { Value = "", Text = "-- Chọn Tỉnh/thành giao hàng --" } };
            }
            else
            {
                provinces[0].Text = "-- Chọn Tỉnh/thành giao hàng --";
            }
            ViewBag.Provinces = provinces;

            return View(input);
        }
        /// <summary>
        /// Lập đơn hàng
        /// </summary>
        /// <param name="customerId"></param>
        /// <param name="province"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Create(int customerId = 0, string province = "", string address = "")
        {
            if (customerId == 0 || string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(province))
            {
                TempData["CreateError"] = "Vui lòng chọn khách hàng, chọn Tỉnh/thành và nhập địa chỉ giao hàng.";
                return RedirectToAction("Create");
            }

            var order = new Order()
            {
                CustomerID = customerId,
                DeliveryProvince = province,
                DeliveryAddress = address,
                OrderTime = DateTime.Now,
                Status = OrderStatusEnum.New
            };

            var orderId = await SalesDataService.AddOrderAsync(order);
            if (orderId <= 0)
            {
                TempData["CreateError"] = "Không thể tạo đơn hàng. Vui lòng thử lại.";
                return RedirectToAction("Create");
            }

            var cart = ShoppingCartHelper.GetShoppingCart();
            if (cart != null && cart.Any())
            {
                foreach (var item in cart)
                {
                    var detail = new OrderDetail()
                    {
                        OrderID = orderId,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        SalePrice = item.SalePrice
                    };
                    await SalesDataService.AddDetailAsync(detail);
                }
            }

            ShoppingCartHelper.ClearCart();
            TempData["Message"] = "Đã lập đơn hàng thành công.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Tìm hàng để bán 
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchProduct(ProductSearchInput input)
        {
            ApplicationContext.SetSessionData(SEARCH_PRODUCT, input); 
            var result = await CatalogDataService.ListProductsAsync(input);

            if (Request.Query.ContainsKey("json") && Request.Query["json"] == "1")
            {
                return Json(result);
            }

            return PartialView("SearchProduct", result);
        }

        /// <summary>
        /// Hiển thị giỏ hàng
        /// </summary>
        public IActionResult ShowCart()
        {
            var cart = ShoppingCartHelper.GetShoppingCart();
            return View(cart); 
        }

        /// <summary>
        /// Thêm hàng vào giỏ=
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddCartItem(int productId = 0, int quantity = 0, decimal price = 0)
        {
            if (productId <= 0)
                return Json(new ApiResult(0, "Mặt hàng không hợp lệ"));
            if (quantity <= 0)
                return Json(new ApiResult(0, "Số lượng phải lớn hơn 0"));
            if (price < 0)
                return Json(new ApiResult(0, "Giá không hợp lệ"));

            var product = await CatalogDataService.GetProductAsync(productId);
            if (product == null)
                return Json(new ApiResult(0, "Mặt hàng không tồn tại"));
            if (!product.IsSelling)
                return Json(new ApiResult(0, "Mặt hàng này đã ngưng bán"));

            var item = new OrderDetailViewInfo()
            {
                ProductID = productId,
                ProductName = product.ProductName,
                Unit = product.Unit,
                Photo = product.Photo,
                Quantity = quantity,
                SalePrice = price
            };
            ShoppingCartHelper.AddItemToCart(item);
            return Json(new ApiResult(1, ""));
        }

        /// <summary>
        /// Chỉnh sửa mục trong giỏ
        /// </summary>
        [HttpGet]
        public IActionResult EditCartItem(int productId)
        {
            var cart = ShoppingCartHelper.GetShoppingCart();
            var item = cart.FirstOrDefault(x => x.ProductID == productId);
            if (item == null)
                return View(new OrderDetailViewInfo());

            return View(item); 
        }

        /// <summary>
        /// Cập nhật giỏ hàng 
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> EditCartItem(int productId, int quantity, decimal price)
        {
            var product = await CatalogDataService.GetProductAsync(productId);
            if (product == null)
                return Json(new ApiResult(0, "Mặt hàng không tồn tại"));

            var item = new OrderDetailViewInfo()
            {
                ProductID = productId,
                ProductName = product.ProductName,
                Unit = product.Unit,
                Photo = product.Photo,
                Quantity = quantity,
                SalePrice = price
            };

            ShoppingCartHelper.UpdateItemInCart(item);
            return Json(new ApiResult(1, ""));
        }

        /// <summary>
        /// Xóa mục giỏ hàng 
        /// </summary>
        [HttpGet]
        public IActionResult DeleteCartItem(int productId)
        {
            ViewBag.ProductID = productId;
            return View(); 
        }

        /// <summary>
        /// Xử lý xóa mục giỏ hàng
        /// </summary>
        [HttpPost, ActionName("DeleteCartItem")]
        public IActionResult DeleteCartItemConfirm(int productId)
        {
            ShoppingCartHelper.RemoveItemFromCart(productId);
            return Json(new ApiResult(1, ""));
        }

        /// <summary>
        /// Xóa toàn bộ giỏ hàng
        /// </summary>
        [HttpGet]
        public IActionResult ClearCart()
        {
            return View(); 
        }

        /// <summary>
        /// Xử lý xóa toàn bộ giỏ hàng
        /// </summary>
        [HttpPost, ActionName("ClearCart")]
        public IActionResult ClearCartConfirm()
        {
            ShoppingCartHelper.ClearCart();
            return Json(new ApiResult(1, ""));
        }

        /// <summary>
        /// Hiển thị chi tiết đơn hàng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Detail(int id = 0, int page = 1)
        {
            if (id <= 0) return RedirectToAction("Index");

            const int pageSize = 10;
            ViewBag.Title = "Chi tiết đơn hàng";

            var model = await SalesDataService.GetOrderAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index"); 
            }
            var allDetails = await SalesDataService.ListDetailsAsync(id) ?? new List<OrderDetailViewInfo>();

            if (allDetails.Any())
            {
                foreach (var detail in allDetails)
                {
                    if (detail.ProductID > 0)
                    {
                        var product = await CatalogDataService.GetProductAsync(detail.ProductID);
                        if (product != null)
                        {
                            detail.ProductName = product.ProductName;
                            detail.Unit = product.Unit;
                            detail.Photo = product.Photo;
                        }
                    }
                }
            }

            int totalItems = allDetails.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            if (totalPages == 0) totalPages = 1; 

            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;
            var pagedDetails = allDetails
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            model.OrderDetails = pagedDetails;
            ViewBag.CurrentPage = page;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;

            return View(model);
        }

        /// <summary>
        /// Xóa đơn hàng
        /// </summary>
        [HttpGet]
        public IActionResult Delete(int id)
        {
            return View(id);
        }
        /// <summary>
        /// Xác nhận xoá đơn hàng
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirm(int id)
        {
            var ok = await SalesDataService.DeleteOrderAsync(id);
            if (ok)
            {
                TempData["Message"] = $"Đã xóa đơn hàng #{id} thành công!";
            }
            else
            {
                TempData["Message"] = $"Không thể xóa đơn hàng #{id}.";
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Giao diện xác nhận Duyệt đơn hàng
        /// </summary>
        [HttpGet]
        public IActionResult Accept(int id)
        {
            return View(id); 
        }

        [HttpPost, ActionName("Accept")]
        public async Task<IActionResult> AcceptConfirm(int id)
        {
            var ok = await SalesDataService.AcceptOrderAsync(id, 0);
            if (!ok)
            {
                TempData["Message"] = "Không thể duyệt đơn hàng.";
            }
            return RedirectToAction("Detail", new { id = id });
        }

        /// <summary>
        /// Chuyển giao hàng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Shipping(int id)
        {
            var input = new PaginationSearchInput() { Page = 1, PageSize = 0, SearchValue = "" };
            var shippersPaged = await PartnerDataService.ListShippersAsync(input);
            var shippers = new List<SelectListItem>() { new SelectListItem { Value = "0", Text = "-- Chọn người giao hàng --" } };
            foreach (var s in shippersPaged.DataItems)
            {
                shippers.Add(new SelectListItem { Value = s.ShipperID.ToString(), Text = s.ShipperName });
            }
            ViewBag.Shippers = shippers;
            return View(id); 
        }
        /// <summary>
        /// Xác nhận chuyển giao hàng
        /// </summary>
        /// <param name="id"></param>
        /// <param name="shipperId"></param>
        /// <returns></returns>
        [HttpPost, ActionName("Shipping")]
        public async Task<IActionResult> ShippingConfirm(int id, int shipperId = 0)
        {
            if (shipperId <= 0)
            {
                TempData["Message"] = "Vui lòng chọn người giao hàng!";
                return RedirectToAction("Detail", new { id = id });
            }

            var current = await SalesDataService.GetOrderAsync(id);
            if (current == null)
            {
                TempData["Message"] = "Đơn hàng không tồn tại.";
                return RedirectToAction("Detail", new { id = id });
            }
            if (current.Status != OrderStatusEnum.Accepted)
            {
                TempData["Message"] = "Đơn hàng phải ở trạng thái 'Đã duyệt' trước khi chuyển giao.";
                return RedirectToAction("Detail", new { id = id });
            }

            var ok = await SalesDataService.ShipOrderAsync(id, shipperId);
            if (!ok)
            {
                TempData["Message"] = "Không thể chuyển giao đơn hàng.";
            }
            return RedirectToAction("Detail", new { id = id });
        }

        /// <summary>
        /// Hoàn tất đơn hàng
        /// </summary>
        [HttpGet]
        public IActionResult Finish(int id)
        {
            return View(id); 
        }
        /// <summary>
        /// Xác nhận hoàn tất
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost, ActionName("Finish")]
        public async Task<IActionResult> FinishConfirm(int id)
        {
            var current = await SalesDataService.GetOrderAsync(id);
            if (current == null)
            {
                TempData["Message"] = "Đơn hàng không tồn tại.";
                return RedirectToAction("Detail", new { id = id });
            }
            if (current.Status != OrderStatusEnum.Shipping)
            {
                TempData["Message"] = "Đơn hàng phải ở trạng thái 'Đang giao' trước khi hoàn tất.";
                return RedirectToAction("Detail", new { id = id });
            }

            var ok = await SalesDataService.CompleteOrderAsync(id);
            if (!ok)
            {
                TempData["Message"] = "Không thể hoàn tất đơn hàng.";
            }
            return RedirectToAction("Detail", new { id = id });
        }

        /// <summary>
        /// Giao diện xác nhận Từ chối đơn hàng
        /// </summary>
        [HttpGet]
        public IActionResult Reject(int id)
        {
            return View(id);
        }

        [HttpPost, ActionName("Reject")]
        public async Task<IActionResult> RejectConfirm(int id)
        {
            var ok = await SalesDataService.RejectOrderAsync(id, 0);
            if (!ok)
            {
                TempData["Message"] = "Không thể từ chối đơn hàng.";
            }
            return RedirectToAction("Detail", new { id = id });
        }

        /// <summary>
        /// Giao diện xác nhận Hủy đơn hàng
        /// </summary>
        [HttpGet]
        public IActionResult Cancel(int id)
        {
            return View(id); 
        }

        [HttpPost, ActionName("Cancel")]
        public async Task<IActionResult> CancelConfirm(int id)
        {
            var ok = await SalesDataService.CancelOrderAsync(id);
            if (!ok)
            {
                TempData["Message"] = "Không thể hủy đơn hàng.";
            }
            return RedirectToAction("Detail", new { id = id });
        }
    }
}