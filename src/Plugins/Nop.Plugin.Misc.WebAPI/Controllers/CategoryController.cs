using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Media;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Seo;
using Nop.Web.Controllers;
using Nop.Web.Infrastructure.Cache;
using Nop.Web.Models.Catalog;
using Nop.Web.Models.Media;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Nop.Plugin.Misc.WebAPI.Controllers
{
    public class CategoryController : BasePublicController
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

        private readonly ICustomerService _customerService;
        public CategoryController(ICategoryService category, IWorkContext workContext,
            MediaSettings mediaSettings, IWebHelper webHelper, IStaticCacheManager cacheManager, IPictureService pictureService, ILocalizationService localizationService, IUrlRecordService urlRecordService, IStoreContext storeContext, ICustomerService customerService)
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

              }
         [HttpGet("api/getCategories")]
        public IActionResult GetCategories(string mobileno)
        {
            var customer = _customerService.GetCustomerByUsername(mobileno);
            _workContext.CurrentCustomer = customer;


            //var pictureSize = _mediaSettings.CategoryThumbPictureSize;

            //var categoriesCacheKey = NopModelCacheDefaults.CategoryHomepageKey.FillCacheKey(
            //    pictureSize,
            //    _workContext.WorkingLanguage.Id,
            //    _webHelper.IsCurrentConnectionSecured());
            var categories = _category.GetAllCategories().Where(m => m.IncludeInTopMenu ==true);
            //var model = _cacheManager.Get(categoriesCacheKey, () =>
            //      categories.Where(c =>c .IncludeInTopMenu == true)
            //          .Select(category =>
            //          {
            //              var catModel = new CategoryModel
            //              {
            //                  Id = category.Id,
            //                  Name = _localizationService.GetLocalized(category, x => x.Name),
            //                  Description = _localizationService.GetLocalized(category, x => x.Description),
            //                  MetaKeywords = _localizationService.GetLocalized(category, x => x.MetaKeywords),
            //                  MetaDescription = _localizationService.GetLocalized(category, x => x.MetaDescription),
            //                  MetaTitle = _localizationService.GetLocalized(category, x => x.MetaTitle),
            //                  SeName = _urlRecordService.GetSeName(category),
            //              };

            //            //prepare picture model
            //            var categoryPictureCacheKey = NopModelCacheDefaults.CategoryPictureModelKey.FillCacheKey(
            //                  category.Id, pictureSize, true, _workContext.WorkingLanguage.Id,
            //                  _webHelper.IsCurrentConnectionSecured(), _storeContext.CurrentStore.Id);
            //              catModel.PictureModel = _cacheManager.Get(categoryPictureCacheKey, () =>
            //              {
            //                  var picture = _pictureService.GetPictureById(category.PictureId);
            //                  var pictureModel = new PictureModel
            //                  {
            //                      FullSizeImageUrl = _pictureService.GetPictureUrl(ref picture),
            //                      ImageUrl = _pictureService.GetPictureUrl(ref picture, pictureSize),
            //                      Title = string.Format(
            //                          _localizationService.GetResource("Media.Category.ImageLinkTitleFormat"),
            //                          catModel.Name),
            //                      AlternateText =
            //                          string.Format(
            //                              _localizationService.GetResource("Media.Category.ImageAlternateTextFormat"),
            //                              catModel.Name)
            //                  };
            //                  return pictureModel;
            //              });

            //              return catModel;
            //          }).ToList());

            return Ok(categories);
        }
    }
}
