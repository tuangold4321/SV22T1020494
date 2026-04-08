using Microsoft.AspNetCore.Mvc;
using SV22T1020494.BusinessLayers;
using SV22T1020494.Models.Sales;
using System.Collections.Generic;
using SV22T1020494.DataLayers.SQLServer;
using SV22T1020494.Shop.Models;

namespace SV22T1020494.Shop.Controllers
{
    /// <summary>
    /// Các chức năng giỏ hàng
    /// </summary>
    public class CartController : Controller
    {
        private readonly CustomerAccountRepository _customerAccountRepo;

        public CartController()
        {
            _customerAccountRepo = new CustomerAccountRepository(Configuration.ConnectionString);
        }
        /// <summary>
        /// Trang giỏ hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            ViewData["AppStartStamp"] = SV22T1020494.Shop.AppInfo.StartStamp;
            return View();
        }
        /// <summary>
        /// Trang xác nhận mua hàng
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Checkout()
        {
            ViewData["AppStartStamp"] = SV22T1020494.Shop.AppInfo.StartStamp;
            var vm = new SV22T1020494.Shop.Models.CartViewModel();
            return View(vm);
        }
        /// <summary>
        /// Xoá theo từng mặt hàng trong giỏ
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult DeleteCartItem(int id)
        {
            return Json(new { success = true, id });
        }

        /// <summary>
        /// Khởi tạo đơn đặt hàng
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, redirectUrl = "/Account/Login" });
            }

            try
            {
                var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                int customerId;
                if (!int.TryParse(idClaim, out customerId))
                {
                    var emailClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                    if (string.IsNullOrWhiteSpace(emailClaim))
                        return Json(new { success = false, message = "Cannot identify customer" });

                    var cid = await _customerAccountRepo.GetCustomerIdByEmailAsync(emailClaim);
                    if (!cid.HasValue)
                        return Json(new { success = false, message = "Cannot identify customer" });

                    customerId = cid.Value;
                }

                var order = new Order
                {
                    CustomerID = customerId,
                    OrderTime = DateTime.Now,
                    DeliveryProvince = request.Province,
                    DeliveryAddress = request.Address,
                    Status = 0 
                };

                int orderId = await SalesDataService.AddOrderAsync(order);
                if (orderId <= 0)
                {
                    return Json(new { success = false, message = "Failed to create order" });
                }

                if (request.Items != null && request.Items.Any())
                {
                    foreach (var item in request.Items)
                    {
                        var detail = new OrderDetail
                        {
                            OrderID = orderId,
                            ProductID = item.ProductID,
                            Quantity = item.Quantity,
                            SalePrice = item.Price
                        };

                        await SalesDataService.AddDetailAsync(detail);
                    }
                }

                return Json(new { success = true, orderId = orderId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
}
}
