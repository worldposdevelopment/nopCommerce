using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.WebAPI.DTO;
using Nop.Plugin.Misc.WebAPI.Filter;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Vendors;
using Nop.Web.Controllers;
using Nop.Web.Factories;
using Nop.Web.Models.Catalog;
using StackExchange.Profiling.Internal;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Nop.Plugin.Misc.WebAPI.Controllers
{
    [ApiKeyAuth]
    [Route("")]
    [ApiController]
    public class CatalogController : ControllerBase
    {

 
        private readonly ICategoryService _categoryService;
        private readonly IVendorService _vendorService;
        private readonly ICatalogModelFactory _catalogModelFactory;
        private readonly IProductService _productService;
        private readonly IProductModelFactory _productModelFactory;
        private readonly ICustomerService _customerService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly IWorkContext _workContext;
        private readonly CatalogSettings _catalogSettings;
        private readonly IReviewTypeService _reviewTypeService;
        private readonly ILocalizationService _localizationService;
        private readonly LocalizationSettings _localizationSettings;
        private readonly IOrderService _orderService;
        private readonly IStoreContext _storeContext;
        private readonly ILogger _logger;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IManufacturerService _manufacturerService;
        private readonly IStaticCacheManager _staticCacheManager;
        public CatalogController(ICategoryService categoryService, IProductService productService, ICatalogModelFactory catalogModelFactory, IProductModelFactory productModelFactory, IVendorService vendorService, ICustomerService customerService, IWorkContext workContext, CatalogSettings catalogSettings, IReviewTypeService reviewTypeService, ILocalizationService localizationService, IOrderService orderService, IStoreContext storeContext, LocalizationSettings localizationSettings, ICustomerActivityService customerActivityService, ILogger logger, IShoppingCartService shoppingCartService, IManufacturerService manufacturerService, IStaticCacheManager staticCacheManager)
        {
            _catalogModelFactory = catalogModelFactory;
            _categoryService = categoryService;
            _productService = productService;
            _productModelFactory = productModelFactory;
            _vendorService = vendorService;
            _customerService = customerService;
            _workContext = workContext;
            _catalogSettings = catalogSettings;
            _reviewTypeService = reviewTypeService;
            _localizationService = localizationService;
            _orderService = orderService;
            _storeContext = storeContext;
            _localizationSettings = localizationSettings;
            _customerActivityService = customerActivityService;
            _logger = logger;
            _shoppingCartService = shoppingCartService;
            _manufacturerService =  manufacturerService;
            _staticCacheManager = staticCacheManager;

        }
        [AllowAnonymous]
        [HttpGet("api/clearcache")]
        public IActionResult ClearCache()
        {
            var customer = _customerService.GetCustomerByUsername("Guest");
            _staticCacheManager.Clear();
            return Ok();
        }
        [HttpGet("api/getmanufacturerproducts")]
        public IActionResult GetManufacturer(int manufacturerid, [FromQuery]CatalogPagingFilteringModel command, string mobileno)
        {

            var customer = _customerService.GetCustomerByUsername(mobileno);
            _workContext.CurrentCustomer = customer;

            var manufacturer = _manufacturerService.GetManufacturerById(manufacturerid);
            var model = _catalogModelFactory.PrepareManufacturerModel(manufacturer, command);

            //template
            //  var templateViewPath = _catalogModelFactory.PrepareCategoryTemplateViewPath(category.CategoryTemplateId);
            return Ok(model);
        }
        [HttpGet("api/getpromoproducts")]
        public IActionResult GetPromoProducts([FromQuery]CatalogPagingFilteringModel command, string mobileno)
        {

            var customer = _customerService.GetCustomerByUsername(mobileno);
            _workContext.CurrentCustomer = customer;

            var vendor = _vendorService.GetVendorById(1);
            var model = _catalogModelFactory.PrepareVendorModel(vendor, command);
            model.Products = model.Products.Where(a => a.ProductPrice.OldPrice != null && a.ProductPrice.OldPrice != a.ProductPrice.Price).ToList();

            //template
            //  var templateViewPath = _catalogModelFactory.PrepareCategoryTemplateViewPath(category.CategoryTemplateId);
            return Ok(model);
        }
        [HttpGet("api/getallproducts")]
        public IActionResult GetVendor(int categoryId, [FromQuery] CatalogPagingFilteringModel command, string mobileno)
        {

            var customer = _customerService.GetCustomerByUsername(mobileno);
            _workContext.CurrentCustomer = customer;

            // var vendor = _vendorService.GetVendorById(1);
            var category = _categoryService.GetCategoryById(2);
            var model = _catalogModelFactory.PrepareCategoryModel(category, command);
           // var model = _catalogModelFactory.PrepareVendorModel(vendor, command);
          //  _logger.Information("GetCatalogReturn: " + model.ToJson(), null, null);

            //template
            //  var templateViewPath = _catalogModelFactory.PrepareCategoryTemplateViewPath(category.CategoryTemplateId);
            return Ok(model);
        }
        [HttpGet("api/getcatalog")]
        public  IActionResult GetCatalog(int categoryId, [FromQuery]CatalogPagingFilteringModel command,string mobileno)
        {
          

            var customer = _customerService.GetCustomerByUsername(mobileno);
            _workContext.CurrentCustomer = customer;

            var category = _categoryService.GetCategoryById(categoryId);
            if (category == null || category.Deleted)
                return BadRequest();

            //var notAvailable =
            //    //published?
            //    !category.Published ||
            //    //ACL (access control list) 
            //    !_aclService.Authorize(category) ||
            //    //Store mapping
            //    !_storeMappingService.Authorize(category);
            ////Check whether the current user has a "Manage categories" permission (usually a store owner)
            ////We should allows him (her) to use "Preview" functionality
            //var hasAdminAccess = _permissionService.Authorize(StandardPermissionProvider.AccessAdminPanel) && _permissionService.Authorize(StandardPermissionProvider.ManageCategories);
            //if (notAvailable && !hasAdminAccess)
            //    return InvokeHttp404();

            ////'Continue shopping' URL
            //_genericAttributeService.SaveAttribute(_workContext.CurrentCustomer,
            //    NopCustomerDefaults.LastContinueShoppingPageAttribute,
            //    _webHelper.GetThisPageUrl(false),
            //    _storeContext.CurrentStore.Id);

            ////display "edit" (manage) link
            //if (_permissionService.Authorize(StandardPermissionProvider.AccessAdminPanel) && _permissionService.Authorize(StandardPermissionProvider.ManageCategories))
            //    DisplayEditLink(Url.Action("Edit", "Category", new { id = category.Id, area = AreaNames.Admin }));

            ////activity log
            //_customerActivityService.InsertActivity("PublicStore.ViewCategory",
            //    string.Format(_localizationService.GetResource("ActivityLog.PublicStore.ViewCategory"), category.Name), category);

            //model
            var model = _catalogModelFactory.PrepareCategoryModel(category, command);
   

            //template
            //  var templateViewPath = _catalogModelFactory.PrepareCategoryTemplateViewPath(category.CategoryTemplateId);
            return Ok(model);
        }
        [HttpGet("api/getproductdetails")]
        public IActionResult GetProductDetails(int productId, string mobileno)
        {
            
                var customer = _customerService.GetCustomerByUsername(mobileno);
                _workContext.CurrentCustomer = customer;

                var product = _productService.GetProductById(productId);
            var model = _productModelFactory.PrepareProductDetailsModel(product, null, false);
            //var stockAvailability = _productService.FormatStockMessage(product, attributeXml);

            //template
            //  var templateViewPath = _catalogModelFactory.PrepareCategoryTemplateViewPath(category.CategoryTemplateId);
            var wishlist = _shoppingCartService.GetShoppingCart(customer, ShoppingCartType.Wishlist, 1, productId, null, null);
            if (wishlist.Count > 0)
                model.InWishlist = true;
            return Ok(model);
        }
        //[HttpGet("api/filtersize")]
        //public virtual IActionResult Filterbysize(SearchModel model, CatalogPagingFilteringModel command, string name, string mobileno)
        //{

        //    var customer = _customerService.GetCustomerByUsername(mobileno);
        //    _workContext.CurrentCustomer = customer;
        //    //'Continue shopping' URL
        //    //_genericAttributeService.SaveAttribute(_workContext.CurrentCustomer,
        //    //    NopCustomerDefaults.LastContinueShoppingPageAttribute,
        //    //    _webHelper.GetThisPageUrl(true),
        //    //    _storeContext.CurrentStore.Id);

        //    if (model == null)
        //        model = new SearchModel();

        //    model = _catalogModelFactory.PrepareSearchByAttributeValueNameModel(model, command, name);
        //    return Ok(model);
        //}
        [HttpGet("api/searchproducts")]
        public virtual IActionResult Search([FromQuery]SearchModel model, [FromQuery]CatalogPagingFilteringModel command, string mobileno)
        {

            var customer = _customerService.GetCustomerByUsername(mobileno);
            _workContext.CurrentCustomer = customer;
            //'Continue shopping' URL
            //_genericAttributeService.SaveAttribute(_workContext.CurrentCustomer,
            //    NopCustomerDefaults.LastContinueShoppingPageAttribute,
            //    _webHelper.GetThisPageUrl(true),
            //    _storeContext.CurrentStore.Id);

            if (model == null)
                model = new SearchModel();

            model = _catalogModelFactory.PrepareSearchModel(model, command);
            return Ok(model);
        }
        [HttpGet("api/productreviews/{productId}")]
        public virtual IActionResult ProductReviews(int productId, string mobileno)
        {
            var customer = _customerService.GetCustomerByUsername(mobileno);
            _workContext.CurrentCustomer = customer;

            var product = _productService.GetProductById(productId);
            if (product == null || product.Deleted || !product.Published || !product.AllowCustomerReviews)
                return NotFound();

            var model = new ProductReviewsModel();
            model = _productModelFactory.PrepareProductReviewsModel(model, product);
            //only registered users can leave reviews
            if (_customerService.IsGuest(_workContext.CurrentCustomer) && !_catalogSettings.AllowAnonymousUsersToReviewProduct)
                ModelState.AddModelError("", _localizationService.GetResource("Reviews.OnlyRegisteredUsersCanWriteReviews"));

            if (_catalogSettings.ProductReviewPossibleOnlyAfterPurchasing)
            {
                var hasCompletedOrders = _orderService.SearchOrders(customerId: _workContext.CurrentCustomer.Id,
                    productId: productId,
                    osIds: new List<int> { (int)OrderStatus.Complete },
                    pageSize: 1).Any();
                if (!hasCompletedOrders)
                    ModelState.AddModelError(string.Empty, _localizationService.GetResource("Reviews.ProductReviewPossibleOnlyAfterPurchasing"));
            }

            //default value
            model.AddProductReview.Rating = _catalogSettings.DefaultProductRatingValue;

            //default value for all additional review types
            if (model.ReviewTypeList.Count > 0)
                foreach (var additionalProductReview in model.AddAdditionalProductReviewList)
                {
                    additionalProductReview.Rating = additionalProductReview.IsRequired ? _catalogSettings.DefaultProductRatingValue : 0;
                }


            return Ok(model);
        }

        [HttpPost("api/addproductreviews/{productId}")]
        public virtual IActionResult ProductReviewsAdd(int productId, [FromBody]ReviewDTO model, string mobileno, bool captchaValid = true)
        {
            var customer = _customerService.GetCustomerByUsername(mobileno);
            _workContext.CurrentCustomer = customer;

            var product = _productService.GetProductById(productId);
            if (product == null || product.Deleted || !product.Published || !product.AllowCustomerReviews)
                return BadRequest();


            if (_customerService.IsGuest(_workContext.CurrentCustomer) && !_catalogSettings.AllowAnonymousUsersToReviewProduct)
            {
                ModelState.AddModelError("", _localizationService.GetResource("Reviews.OnlyRegisteredUsersCanWriteReviews"));
            }

            if (_catalogSettings.ProductReviewPossibleOnlyAfterPurchasing)
            {
                var hasCompletedOrders = _orderService.SearchOrders(customerId: _workContext.CurrentCustomer.Id,
                    productId: productId,
                    osIds: new List<int> { (int)OrderStatus.Complete },
                    pageSize: 1).Any();
                if (!hasCompletedOrders)
                    ModelState.AddModelError(string.Empty, _localizationService.GetResource("Reviews.ProductReviewPossibleOnlyAfterPurchasing"));
            }

            if (ModelState.IsValid)
            {
                //save review
                var rating = model.Rating;
                if (rating < 1 || rating > 5)
                    rating = _catalogSettings.DefaultProductRatingValue;
                var isApproved = !_catalogSettings.ProductReviewsMustBeApproved;

                var productReview = new ProductReview
                {
                    ProductId = product.Id,
                    CustomerId = _workContext.CurrentCustomer.Id,
                    Title = model.Title,
                    ReviewText = model.ReviewText,
                    Rating = rating,
                    HelpfulYesTotal = 0,
                    HelpfulNoTotal = 0,
                    IsApproved = isApproved,
                    CreatedOnUtc = DateTime.UtcNow,
                    StoreId = _storeContext.CurrentStore.Id,
                };

                _productService.InsertProductReview(productReview);

                ////add product review and review type mapping                
                //foreach (var additionalReview in model.AddAdditionalProductReviewList)
                //{
                //    var additionalProductReview = new ProductReviewReviewTypeMapping
                //    {
                //        ProductReviewId = productReview.Id,
                //        ReviewTypeId = additionalReview.ReviewTypeId,
                //        Rating = additionalReview.Rating
                //    };

                //    _reviewTypeService.InsertProductReviewReviewTypeMappings(additionalProductReview);
                //}

                //update product totals
                _productService.UpdateProductReviewTotals(product);

                //notify store owner
                if (_catalogSettings.NotifyStoreOwnerAboutNewProductReviews)
                    _workflowMessageService.SendProductReviewNotificationMessage(productReview, _localizationSettings.DefaultAdminLanguageId);

                //activity log
                _customerActivityService.InsertActivity("PublicStore.AddProductReview",
                    string.Format(_localizationService.GetResource("ActivityLog.PublicStore.AddProductReview"), product.Name), product);

                //    //raise event
                //    if (productReview.IsApproved)
                //        _eventPublisher.Publish(new ProductReviewApprovedEvent(productReview));

                //    model = _productModelFactory.PrepareProductReviewsModel(model, product);
                //    model.AddProductReview.Title = null;
                //    model.AddProductReview.ReviewText = null;

                //    model.AddProductReview.SuccessfullyAdded = true;
                //    if (!isApproved)
                //        model.AddProductReview.Result = _localizationService.GetResource("Reviews.SeeAfterApproving");
                //    else
                //        model.AddProductReview.Result = _localizationService.GetResource("Reviews.SuccessfullyAdded");

                //    return Ok(model);
                //}

                ////If we got this far, something failed, redisplay form
                //model = _productModelFactory.PrepareProductReviewsModel(model, product);
                //return Ok(model);
            }
            return Ok(model);
        }

    }
}
    