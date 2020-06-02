using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.WebAPI.Models.Promotions;
using Nop.Services.Catalog;
using Nop.Web.Factories;
using Nop.Web.Models.Catalog;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Nop.Plugin.Misc.WebAPI.Controllers
{
  
    public class PromotionsController : Controller
    {
        private readonly ICatalogModelFactory _catalogModelFactory;
        private readonly ICategoryService _categoryService;
        public PromotionsController(ICatalogModelFactory catalogModelFactory, ICategoryService categoryService)
        {
            _catalogModelFactory = catalogModelFactory;
            _categoryService = categoryService;

        }
        // GET: /<controller>/
        [AllowAnonymous]
        [HttpGet("api/homesliders")]
        public ActionResult GetHomeSliders()
        {
            CatalogPagingFilteringModel command = new CatalogPagingFilteringModel();
            var category = _categoryService.GetCategoryById(3);
        
            var model = _catalogModelFactory.PrepareCategoryModel(category, command);
            var featuredProducts = model.Products;
        List <HomeSlider> top = new List<HomeSlider>();
            top.Add(new HomeSlider
            {
                CategoryId = 6,
                ListingOrder = 1,
                Name = "Signature Players",
                Picture = "https://i.ibb.co/BsvH26C/9.jpg"


            });
            top.Add(new HomeSlider
            {
                CategoryId = 3,
                ListingOrder = 1,
                Name = "Sneakers",
                Picture = "https://i.ibb.co/hZjHKSz/5.jpg"


            });
            List<HomeSlider> bottom = new List<HomeSlider>();
            bottom.Add(new HomeSlider
            {
                CategoryId = 7,
                ListingOrder = 1,
                Name = "Apparel",
                Picture = "https://i.ibb.co/941S9n5/4.jpg"


            });
            bottom.Add(new HomeSlider
            {
                CategoryId = 9,
                ListingOrder = 1,
                Name = "New Release",
                Picture = "https://i.ibb.co/kcKHNhd/2.jpg"


            });



            return Ok(new { top, bottom, featuredProducts });
        }
    }
}
