using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Media;
using Nop.Plugin.Misc.WebAPI.Filter;
using Nop.Services.Caching;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Seo;
using Nop.Web.Controllers;
using Nop.Web.Infrastructure.Cache;
using Nop.Web.Models.Catalog;
using Nop.Web.Models.Media;
using static Nop.Web.Models.Catalog.CategoryModel;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Nop.Plugin.Misc.WebAPI.Controllers
{
    [ApiKeyAuth]
    [Route("")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _category;
        private readonly IWorkContext _workContext;
        private readonly MediaSettings _mediaSettings;
        private readonly IWebHelper _webHelper;
        private readonly ILocalizationService _localizationService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IStaticCacheManager _cacheManager;
        private readonly IPictureService _pictureService;
        private readonly IStoreContext _storeContext;
        private readonly ICacheKeyService _cacheKeyService;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly IManufacturerService _manufacturerService;

        private readonly ICustomerService _customerService;
        public CategoryController(ICategoryService category, IWorkContext workContext,
            MediaSettings mediaSettings, IWebHelper webHelper, IStaticCacheManager cacheManager, IPictureService pictureService, ILocalizationService localizationService, IUrlRecordService urlRecordService, IStoreContext storeContext, ICustomerService customerService, ICacheKeyService cacheKeyService, IStaticCacheManager staticCacheManager,  IManufacturerService manufacturerService)
             {
            _workContext = workContext;
            _category = category;
            _mediaSettings = mediaSettings;
            _webHelper = webHelper;
            _cacheManager = cacheManager;
            _pictureService = pictureService;
            _storeContext = storeContext;
            _localizationService = localizationService;
            _urlRecordService = urlRecordService;
            _customerService = customerService;
            _cacheKeyService = cacheKeyService;
            _staticCacheManager =  staticCacheManager;
            _manufacturerService = manufacturerService;

        }
         [HttpGet("api/getCategories")]
        public IActionResult GetCategories(string mobileno)
        {
            var customer = _customerService.GetCustomerByUsername(mobileno);
            _workContext.CurrentCustomer = customer;
            var categories = _category.GetAllCategories().Where(m => m.IncludeInTopMenu ==true);
            var pictureSize = _mediaSettings.ManufacturerThumbPictureSize;

            var manufacturersmodel = new List<ManufacturerModel>();
            var manufacturers = _manufacturerService.GetAllManufacturers(storeId: _storeContext.CurrentStore.Id);
            foreach (var manufacturer in manufacturers)
            {
                var modelMan = new ManufacturerModel
                {
                    Id = manufacturer.Id,
                    Name = _localizationService.GetLocalized(manufacturer, x => x.Name),
                    Description = _localizationService.GetLocalized(manufacturer, x => x.Description),
                    MetaKeywords = _localizationService.GetLocalized(manufacturer, x => x.MetaKeywords),
                    MetaDescription = _localizationService.GetLocalized(manufacturer, x => x.MetaDescription),
                    MetaTitle = _localizationService.GetLocalized(manufacturer, x => x.MetaTitle),
                    SeName = _urlRecordService.GetSeName(manufacturer),
                };

                //prepare picture model
             
                var manufacturerPictureCacheKey = _cacheKeyService.PrepareKeyForDefaultCache(NopModelCacheDefaults.ManufacturerPictureModelKey, 
                    manufacturer, pictureSize, true, _workContext.WorkingLanguage, 
                    _webHelper.IsCurrentConnectionSecured(), _storeContext.CurrentStore);
                modelMan.PictureModel = _staticCacheManager.Get(manufacturerPictureCacheKey, () =>
                {
                    var picture = _pictureService.GetPictureById(manufacturer.PictureId);
                    var pictureModel = new PictureModel
                    {
                        FullSizeImageUrl = _pictureService.GetPictureUrl(ref picture),
                        ImageUrl = _pictureService.GetPictureUrl(ref picture, pictureSize),
                        Title = string.Format(_localizationService.GetResource("Media.Manufacturer.ImageLinkTitleFormat"), modelMan.Name),
                        AlternateText = string.Format(_localizationService.GetResource("Media.Manufacturer.ImageAlternateTextFormat"), modelMan.Name)
                    };

                    return pictureModel;
                });

                manufacturersmodel.Add(modelMan);
            }

            var signatureplayer = _category.GetAllCategories().Where(m => m.ParentCategoryId == 111);
             List<CategoryModel.SubCategoryModel> signatureplayercategories = signatureplayer.Select(curCategory =>
            {
                var subCatModel = new CategoryModel.SubCategoryModel
                {
                    Id = curCategory.Id,
                    Name = _localizationService.GetLocalized(curCategory, y => y.Name),
                    SeName = _urlRecordService.GetSeName(curCategory),
                    Description = _localizationService.GetLocalized(curCategory, y => y.Description)
                };

                //prepare picture model
                var categoryPictureCacheKey = _cacheKeyService.PrepareKeyForDefaultCache(NopModelCacheDefaults.CategoryPictureModelKey, curCategory,
                    pictureSize, true, _workContext.WorkingLanguage, _webHelper.IsCurrentConnectionSecured(),
                    _storeContext.CurrentStore);

                subCatModel.PictureModel = _staticCacheManager.Get(categoryPictureCacheKey, () =>
                {
                    var picture = _pictureService.GetPictureById(curCategory.PictureId);
                    var pictureModel = new PictureModel
                    {
                        FullSizeImageUrl = _pictureService.GetPictureUrl(ref picture),
                        ImageUrl = _pictureService.GetPictureUrl(ref picture, pictureSize),
                        Title = string.Format(
                            _localizationService.GetResource("Media.Category.ImageLinkTitleFormat"),
                            subCatModel.Name),
                        AlternateText =
                            string.Format(
                                _localizationService.GetResource("Media.Category.ImageAlternateTextFormat"),
                                subCatModel.Name)
                    };

                    return pictureModel;
                });

                return subCatModel;
            }).ToList();

            return Ok(new { categories, manufacturersmodel, signatureplayercategories });
        }

    }
}
