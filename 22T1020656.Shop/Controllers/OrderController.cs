using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020656.BusinessLayers;
using SV22T1020656.Models.Sales;

namespace SV22T1020656.Shop.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {

        [HttpGet]
        public async Task<IActionResult> MyOrders(int page = 1, string searchValue = "")
        {
           
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim == null) return RedirectToAction("Login", "Account");

            int customerId = int.Parse(userIdClaim.Value);

           
            var input = new OrderSearchInput()
            {
                Page = page,
                PageSize = 10, 
                SearchValue = searchValue,
                Status = 0,    
                DateFrom = null,
                DateTo = null,
                CustomerID = customerId
                
            };

            
            var result = await SalesDataService.ListOrdersAsync(input);

            var data = result?.DataItems ?? new List<OrderViewInfo>();

            
            return View(data);
        }
    }
}
