using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Media;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Web.Areas.Admin.Models.Orders;
using Nop.Web.Controllers;
using Nop.Web.Factories;
using Nop.Web.Infrastructure.Cache;
using Nop.Web.Models.Media;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Nop.Plugin.Misc.WebAPI.Controllers
{
    public class OrderController : BasePublicController
    {
        private readonly ICustomerService _customerService;
        private readonly IWorkContext _workContext;
        private readonly IOrderModelFactory _orderModelFactory;
        private readonly IOrderService _orderService;
        private readonly IPictureService _pictureService;
        private readonly MediaSettings _mediaSettings;
        private readonly IProductService _productService;
        private readonly ILocalizationService _localizationService;
        public OrderController(ICustomerService customerService, IWorkContext workContext, IOrderModelFactory orderModelFactory, IOrderService orderService, IPictureService pictureService, MediaSettings mediaSettings, IProductService productService, ILocalizationService localizationService)
        {
            _customerService = customerService;
            _workContext = workContext;
            _orderModelFactory = orderModelFactory;
            _orderService = orderService;
            _pictureService = pictureService;
            _mediaSettings = mediaSettings;
            _productService = productService;
            _localizationService = localizationService;
    }
        // GET: /<controller>/
        public IActionResult Index()
        {

            var model = _orderModelFactory.PrepareCustomerOrderListModel();
            return View(model);
        }
        [HttpGet("api/orders")]
        public IActionResult Orders(String mobileno)
        {
            var customer = _customerService.GetCustomerByUsername(mobileno);
            _workContext.CurrentCustomer = customer;

            var model = _orderModelFactory.PrepareCustomerOrderListModel();
            return Ok(model);

        }
        [HttpGet("api/orderdetails")]
        public IActionResult OrderDetails(String mobileno, int orderid)
        {
            var customer = _customerService.GetCustomerByUsername(mobileno);
            _workContext.CurrentCustomer = customer;

            var order = _orderService.GetOrderById(orderid);
            if (order == null || order.Deleted || _workContext.CurrentCustomer.Id != order.CustomerId)
                return Challenge();

            var model = _orderModelFactory.PrepareOrderDetailsModel(order);
            foreach (Web.Models.Order.OrderDetailsModel.OrderItemModel oim in model.Items)
            {

                oim.Picture = PrepareCartItemPictureModel(oim.ProductId,
                       _mediaSettings.CartThumbPictureSize, true, oim.ProductName);

            }

            return Ok(model);

        }
        public virtual PictureModel PrepareCartItemPictureModel(int productid, int pictureSize, bool showDefaultPicture, string productName)
        {

            var product = _productService.GetProductById(productid);

            //shopping cart item picture
            var sciPicture = _pictureService.GetProductPicture(product, null);

            return new PictureModel
            {
                ImageUrl = _pictureService.GetPictureUrl(ref sciPicture, pictureSize, showDefaultPicture),
                Title = string.Format(_localizationService.GetResource("Media.Product.ImageLinkTitleFormat"), productName),
                AlternateText = string.Format(_localizationService.GetResource("Media.Product.ImageAlternateTextFormat"), productName),
            };
        }
    }
}
         
        
    

