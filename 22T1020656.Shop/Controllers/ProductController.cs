using Microsoft.AspNetCore.Mvc;
using SV22T1020656.BusinessLayers;
using SV22T1020656.Models.Catalog;
using SV22T1020656.Models.Common;
using SV22T1020656.Shop;
using System.Threading.Tasks;

namespace _22T1020656.Shop.Controllers
{
    public class ProductController : Controller
    {
        private const String valued_search = "ProductSearchInput";

        public async Task<IActionResult> Index(int categoryID =0)
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(valued_search);
            if (input == null)
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize, // Ví dụ: 20
                    SearchValue = "",
                    CategoryID = 0,
                    SupplierID = 0
                };

            if (categoryID > 0)
            {
                input.CategoryID = categoryID;
                input.Page = 1; // Reset về trang 1
            }

            await LoadSelectListAsync();
            return View(input);
        }


        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            
            var result = await CatalogDataService.ListProductAsync(input);
          
            ApplicationContext.SetSessionData(valued_search, input);

           
            return PartialView("Search", result);
        }

        private async Task LoadSelectListAsync()
        {
            // Category
            var categories = await CatalogDataService.ListCategory(
                new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = 1000,
                    SearchValue = ""
                });

            // Supplier
            var suppliers = await PartnerDataService.ListSuppliersAsync(
                new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = 1000,
                    SearchValue = ""
                });

            
            ViewBag.Categories = categories.DataItems;
            ViewBag.Suppliers = suppliers.DataItems;
        }

        public async Task<IActionResult> Detail(int id)
        {
            var product = await CatalogDataService.GetProductAsync(id);

            if (product == null)
                return RedirectToAction("Index");

            
            ViewBag.Photos = await CatalogDataService.ListProductPhotosAsync(id);
            ViewBag.Attributes = await CatalogDataService.ListProductAttributesAsync(id);

            
            ViewBag.RelatedProducts = await CatalogDataService.ListProductAsync(new ProductSearchInput()
            {
                Page = 1,
                PageSize = 8,
                CategoryID = product.CategoryID ?? 0,
                SearchValue = ""
            });

            return View(product);
        }
    }
}
