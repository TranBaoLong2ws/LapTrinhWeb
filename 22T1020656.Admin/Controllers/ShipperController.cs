using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020656.Admin;
using SV22T1020656.BusinessLayers;
using SV22T1020656.Models.Common;
using SV22T1020656.Models.Partner;
using System.Threading.Tasks;

namespace SV22T1020656.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class ShipperController : Controller
    {

        private const String valued_search = "ShipperSearchInput";

        /// <summary>
        /// hiện thị danh sách nhà cung cấp
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(valued_search);
            if (input == null)
                input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };

            return View(input);
        }


        /// <summary>
        /// hiện thị danh sách khách hàng nhập đầu vào tìm kiếm -> hiển thị kết quả tìm kiếm 
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await PartnerDataService.ListShippersAsync(input);

            ApplicationContext.SetSessionData(valued_search,input);

            return View(result);
        }



        /// <summary>
        /// tạo đơn vị vận chuyển
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Thêm khách hàng";
            var model = new Shipper()
            {
                ShipperID = 0
            };
            return View(new Shipper());
        }



        /// <summary>
        /// 
        /// cập nhập dơn vị vận chuyển
        /// </summary>
        /// <param name="id">cập nhập đơn vị vận chuyển</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật khách hàng";

            var model = await PartnerDataService.GetShipperAsync(id);

            if (model == null)
                return RedirectToAction("Index");


            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> SaveData(Shipper data)
        {
            try
            {
                ViewBag.Title = data.ShipperID == 0 ? "Thêm người giao hàng" : "Cập nhật người giao hàng";
 

                // Validate
                if (String.IsNullOrWhiteSpace(data.ShipperName))
                    ModelState.AddModelError("CustomerName", "Vui lòng nhập tên khách hàng");

                if (String.IsNullOrWhiteSpace(data.Phone))
                    ModelState.AddModelError(nameof(data.Phone), "Vui lòng điền số điện thoại");


                // Nếu lỗi → quay lại Edit
                if (!ModelState.IsValid)
                {
                    return View("Edit", data);
                }

                // Thêm hoặc cập nhật
                if (data.ShipperID == 0)
                {
                    await PartnerDataService.AddShipperAsync(data);
                }
                else
                {
                    await PartnerDataService.UpdateShipperAsync(data);
                }

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                ModelState.AddModelError("Error", "Hệ thông đang lỗi, vui lòng thử lại sau");
                ViewBag.Provinces = await SelectListHelper.Provinces();
                return View("Edit",data);
            }
        }



        /// <summary>
        /// xóa đơn vị vận chuyển
        /// </summary>
        /// <param name="id">mã đơn vị vận chuyển để xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await PartnerDataService.DeleteShipperAsync(id);
                return RedirectToAction("Index");
            }


            var model = await PartnerDataService.GetShipperAsync(id);

            if (model == null)
            {
                return RedirectToAction("Index");
            }

            ViewBag.CanDelete = !await PartnerDataService.IsUsedShipperAsync(id);

            return View(model);
        }
    }
}
