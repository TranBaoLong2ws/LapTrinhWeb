using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020656.Admin;
using SV22T1020656.BusinessLayers;
using SV22T1020656.Models.Common;
using SV22T1020656.Models.HR;
using SV22T1020656.Models.Partner;
using System.Threading.Tasks;

namespace SV22T1020656.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class SupplierController : Controller
    {


        private const String valued_search = "SupplierSearchInput";


        /// <summary>
        /// hiện thị danh sách nhà cung cấp
        /// </summary>
        /// <returns></returns>
        /// 
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
        /// tìm kiếm và trả về kết quả
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await PartnerDataService.ListSuppliersAsync(input);

            ApplicationContext.SetSessionData(valued_search,input);

            return View(result);
        }

      
        /// <summary>
        /// tạo nhà cung cấp
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Thêm nhà cung cấp";
            var model = new Supplier()
            {
                SupplierID = 0
            };
            return View(new Supplier());
        }

        /// <summary>
        /// cập nhập nhà cung cấp
        /// </summary>
        /// <param name="id">mã nhà cung cấp cần cập nhập</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật khách hàng";

            var model = await PartnerDataService.GetSupplierAsync(id);

            if (model == null)
                return RedirectToAction("Index");


            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Supplier data)
        {
            try
            {
                ViewBag.Title = data.SupplierID == 0 ? "Bổ sung nhà cung cấp" : "Cập nhật thông tin nhà cung cấp";

              
                if (string.IsNullOrWhiteSpace(data.SupplierName))
                    ModelState.AddModelError(nameof(data.SupplierName), "Vui lòng nhập họ tên nhà cung cấp");

                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email nhà cung cấp");

                if (!ModelState.IsValid)
                    return View("Edit", data);
             

                
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (String.IsNullOrEmpty(data.ContactName)) data.ContactName = data.SupplierName;


                
                if (data.SupplierID == 0)
                {
                    await PartnerDataService.AddSupplierAsync(data);
                }
                else
                {
                    await PartnerDataService.UpdateSupplierAsync(data);
                }
                return RedirectToAction("Index");
            }
            catch //(Exception ex)
            {
                //TODO: Ghi log lỗi căn cứ vào ex.Message và ex.StackTrace
                ModelState.AddModelError(string.Empty, "Hệ thống đang bận hoặc dữ liệu không hợp lệ. Vui lòng kiểm tra dữ liệu hoặc thử lại sau");
                return View("Edit", data);
            }
        }


        /// <summary>
        /// xóa nhà cung cấp 
        /// </summary>
        /// <param name="id">mã nhà cung cấp để xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await PartnerDataService.DeleteSupplierAsync(id);
                return RedirectToAction("Index");
            }


            var model = await PartnerDataService.GetSupplierAsync(id);

            if (model == null)
            {
                return RedirectToAction("Index");
            }

            ViewBag.CanDelete = !await PartnerDataService.IsUsedSupplierAsync(id);

            return View(model);
        }
    }
}
