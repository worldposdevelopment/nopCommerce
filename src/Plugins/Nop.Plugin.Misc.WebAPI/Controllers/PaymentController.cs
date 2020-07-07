using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Misc.WebAPI.DTO;
using Nop.Plugin.Misc.WebAPI.Filter;
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
    [ApiKeyAuth]
    [Route("")]
    [ApiController]
    public class PaymentContoller : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly IWorkContext _workContext;
        private readonly IOrderModelFactory _orderModelFactory;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IPictureService _pictureService;
        private readonly MediaSettings _mediaSettings;
        private readonly IProductService _productService;
        private readonly ILocalizationService _localizationService;
        private readonly IPriceFormatter _priceFormatter;
        public PaymentContoller(ICustomerService customerService, IWorkContext workContext, IOrderModelFactory orderModelFactory, IOrderService orderService, IPictureService pictureService, MediaSettings mediaSettings, IProductService productService, ILocalizationService localizationService, IOrderProcessingService orderProcessingService, IPriceFormatter priceFormatter)
        {
            _customerService = customerService;
            _workContext = workContext;
            _orderModelFactory = orderModelFactory;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _pictureService = pictureService;
            _mediaSettings = mediaSettings;
            _productService = productService;
            _localizationService = localizationService;
            _priceFormatter = priceFormatter;
    }
 
        [HttpGet("api/paymentcallback")]
        public IActionResult PaymentCallback(string mobilenumber, string orderguid)
        {
            var customer = _customerService.GetCustomerByUsername(mobilenumber);
            _workContext.CurrentCustomer = customer;
            var order = _orderService.GetOrderByGuid(new Guid(orderguid));
            //var order = _orderService.getorder
            if (order.PaymentStatus == PaymentStatus.Paid)
            {
              
                return Ok(new PaymentDetailsDTO { ordernumber = order.CustomOrderNumber, total = _priceFormatter.FormatPrice(order.OrderTotal), mobileno = customer.Username, status = true, transactionid = order.CaptureTransactionId, paymentmethod = order.CardType });
            }
          else
            { 
                if(order.OrderStatus == OrderStatus.Pending)
                { 
                _orderService.InsertOrderNote(new OrderNote
                {
                    OrderId = order.Id,
                    Note = "Payment Exited: ",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });
               
                _orderService.UpdateOrder(order);
            _orderProcessingService.CancelOrder(order, false);
                }

                return Ok(new PaymentDetailsDTO { ordernumber = order.CustomOrderNumber, total = _priceFormatter.FormatPrice(order.OrderTotal), mobileno = customer.Username, status = false, transactionid = order.CaptureTransactionId, paymentmethod = order.CardType });
            }
          

        }
        [HttpGet("api/paymentstatus")]
        public IActionResult SetPaymentStatus(bool success, string ordernumber, string authcode, string transactionid, string paymentmethod)
        {
          
            if (success)
            {
                var order = _orderService.GetOrderByCustomOrderNumber(ordernumber);

                if (order == null)
                    return NotFound();
                var customer = _customerService.GetCustomerById(order.CustomerId);
                _workContext.CurrentCustomer = customer;

                //order note
                _orderService.InsertOrderNote(new OrderNote
                {
                    OrderId = order.Id,
                    Note = "Payment callback",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });

                //validate order total
                //var orderTotalSentToPayPal = _genericAttributeService.GetAttribute<decimal?>(order, EghlHelper.OrderTotalSentToPayPal);
                //if (orderTotalSentToPayPal.HasValue && mcGross != orderTotalSentToPayPal.Value)
                //{
                //    var errorStr = $"PayPal PDT. Returned order total {mcGross} doesn't equal order total {order.OrderTotal}. Order# {order.Id}.";
                //    //log
                //    _logger.Error(errorStr);
                //    //order note
                //    _orderService.InsertOrderNote(new OrderNote
                //    {
                //        OrderId = order.Id,
                //        Note = errorStr,
                //        DisplayToCustomer = false,
                //        CreatedOnUtc = DateTime.UtcNow
                //    });

                //    return RedirectToAction("Index", "Home", new { area = string.Empty });
                //}

                //clear attribute
                //if (orderTotalSentToPayPal.HasValue)
                //    _genericAttributeService.SaveAttribute<decimal?>(order, EghlHelper.OrderTotalSentToPayPal, null);

                //if (newPaymentStatus != PaymentStatus.Paid)
                //    return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });

                if (!_orderProcessingService.CanMarkOrderAsPaid(order))
                    return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });

                //mark order as paid
                order.AuthorizationTransactionId = authcode;
                order.CaptureTransactionId = transactionid;
                order.CardType = paymentmethod;
                _orderService.UpdateOrder(order);
                _orderProcessingService.MarkOrderAsPaid(order);
                return Ok(new PaymentDetailsDTO { ordernumber = ordernumber, total = _priceFormatter.FormatPrice(order.OrderTotal), mobileno = customer.Username, status = true });
   
            }
            else
            {
                //if (!values.TryGetValue("custom", out var orderNumber))
                //    orderNumber = _webHelper.QueryString<string>("cm");

                //var orderNumberGuid = Guid.Empty;

                //try
                //{
                //    orderNumberGuid = new Guid(orderNumber);
                //}
                //catch
                //{
                //    // ignored
                //}

                var order = _orderService.GetOrderByCustomOrderNumber(ordernumber);
                if (order == null)
                    return BadRequest();
                var customer = _customerService.GetCustomerById(order.CustomerId);
                _workContext.CurrentCustomer = customer;

                //order note
                _orderService.InsertOrderNote(new OrderNote
                {
                    OrderId = order.Id,
                    Note = "PaymentFailed: Bad Payment Attempted",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });
                order.AuthorizationTransactionId = authcode;
                order.CaptureTransactionId = transactionid;
                order.CardType = paymentmethod;
                _orderService.UpdateOrder(order);
          
                _orderProcessingService.CancelOrder(order, false);
              
                return Ok(new PaymentDetailsDTO { ordernumber = ordernumber, total = _priceFormatter.FormatPrice(order.OrderTotal), mobileno = customer.Username, status = false });
            }
        }

    }
}
         
        
    

