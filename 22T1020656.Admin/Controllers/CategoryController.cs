using Microsoft.AspNetCore.Mvc;
using SV22T1020656.Admin;
using SV22T1020656.BusinessLayers;
using SV22T1020656.Models.Catalog;
using SV22T1020656.Models.Common;
using SV22T1020656.Models.Partner;
using System.Threading.Tasks;

namespace SV22T1020656.Admin.Controllers
{
    public class CategoryController : Controller
    {

        private const String valued_search = "CategorySearchInput";


        /// <summary>
        /// hiện thị danh sách, danh mục sản phẩm
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
        /// tìm kiếm và trả về kết quả
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await CatalogDataService.ListCategory(input);

            ApplicationContext.SetSessionData(valued_search,input);

            return View(result);

        }

        /// <summary>
        /// Sủa danh mục sản phẩm
        /// </summary>
        /// <param name="id"> mã danh mục sản phẩm cần cập nhập </param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật khách hàng";

            var model = await CatalogDataService.GetCategoryAsync(id);

            if (model == null)
                return RedirectToAction("Index");


            return View(model);
        }

        /// <summary>
        /// tạo danh mục sản phẩm
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Thêm khách hàng";
            var model = new Category()
            {
                CategoryID = 0
            };
            return View(new Category());
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Category data)
        {
            try
            {
                ViewBag.Title =   data.CategoryID == 0 ? "Thêm loại hàng" : "Cập nhật loại hàng hàng";
              

                // Validate
                if (String.IsNullOrWhiteSpace( data.CategoryName))
                    ModelState.AddModelError(nameof(data.CategoryName), "Vui lòng nhập tên loại hàng");


                // Nếu lỗi → quay lại Edit
                if (!ModelState.IsValid)
                {                 
                    return View("Edit", data);
                }

                // Chuẩn hóa dữ liệu
                if (String.IsNullOrWhiteSpace(data.Description))
                    data.Description = "";
                        

                // Thêm hoặc cập nhật
                if ( data.CategoryID == 0)
                {
                    await CatalogDataService.AddCategoryAsync( data);
                }
                else
                {
                    await CatalogDataService.UpdateCategoryAsync(data);
                }

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                ModelState.AddModelError("Error", "Hệ thông đang lỗi, vui lòng thử lại sau");
                ViewBag.Provinces = await SelectListHelper.Provinces();
                return View("Edit", data);
            }
        }

        /// <summary>
        /// xóa danh mục sản phẩm
        /// </summary>
        /// <param name="id"> mã danh mục sản phẩm cần xóa </param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await CatalogDataService.DeleteCategoryAsync(id);
                return RedirectToAction("Index");
            }


            var model = await CatalogDataService.GetCategoryAsync(id);

            if (model == null)
            {
                return RedirectToAction("Index");
            }

            ViewBag.CanDelete = !await CatalogDataService.IsIsUsed(id);

            return View(model);
        }
    }
}
