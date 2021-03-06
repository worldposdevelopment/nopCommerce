﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Misc.WebAPI.DTO;
using Nop.Plugin.Misc.WebAPI.Filter;
using Nop.Plugin.Misc.WebAPI.Models.Smart;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Web.Areas.Admin.Models.Orders;
using Nop.Web.Controllers;
using Nop.Web.Factories;
using Nop.Web.Infrastructure.Cache;
using Nop.Web.Models.Media;
using Org.BouncyCastle.Asn1.Esf;

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
        private readonly IAddressService _addressService;
        public PaymentContoller(ICustomerService customerService, IWorkContext workContext, 
            IOrderModelFactory orderModelFactory, IOrderService orderService, IPictureService pictureService, MediaSettings mediaSettings, 
            IProductService productService, ILocalizationService localizationService, IOrderProcessingService orderProcessingService,
            IPriceFormatter priceFormatter,
            IAddressService addressService)
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
            _addressService = addressService;
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
                    Note = "Callback without payment: ",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });
               
           //     _orderService.UpdateOrder(order);
          //  _orderProcessingService.CancelOrder(order, false);
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

                if (!_orderProcessingService.CanMarkOrderAsPaid(order))
                    return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });

                //mark order as paid
                order.AuthorizationTransactionId = authcode;
                order.CaptureTransactionId = transactionid;
                order.CardType = paymentmethod;
                _orderService.UpdateOrder(order);
                _orderProcessingService.MarkOrderAsPaid(order);

                //        string ShipToAddress1;
                //        string ShipToAddress2;
                //        string ShipToAddress3;
                //        string ShipToAddress4;

                //Address shippingAddress = null;
                //        if (order.ShippingAddressId.HasValue)
                //        {
                //            shippingAddress = _addressService.GetAddressById(order.ShippingAddressId.Value);
                //            if(!String.IsNullOrEmpty(shippingAddress.Address1))
                //            ShipToAddress1 = shippingAddress.Address1;
                //            if (!String.IsNullOrEmpty(shippingAddress.Address2))
                //                ShipToAddress2 = shippingAddress.Address2;
                //            if (!String.IsNullOrEmpty(shippingAddress.Address2))
                //                ShipToAddress2 = shippingAddress.Address2;

                //        }
            
                var smartDo = new SmartDo
                {
                    DocumentHeader = new Documentheader
                    {
                        AcCusInvoiceMID = order.CustomOrderNumber,
                        AcLocationID = "APP",
                        DocumentDate = "/Date(" + Convert.ToInt64((DateTime.Now.Date - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds) + ")/",
                        DeliveryDate = "/Date(" + Convert.ToInt64((DateTime.Now.AddDays(7).Date - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds) + ")/",
                        DocumentNetAmount = order.OrderTotal,
                        DocumentCentBalance = 0,
                        DocumentFinalAmount = order.OrderTotal,
                        ShipToAddress1 = "",
                        ShipToAddress2 = "",
                        ShipToAddress3 = "",
                        ShipToAddress4 = "",
                        ShipToAttention = "",
                        ShipToContact1 = "",
                        ShipToContact2 = "",
                        DocumentRemark = "",
                        RefDocumentNo = "",
                        DocumentYourReference = "",
                        ExtraRemark1 = "",
                        ExtraRemark2 = "",
                        ExtraRemark3 = "",
                        ExtraRemark4 = "",
                        ShipToFax = "",
                        ShipToPhone1 = "",
                        ShipToPhone2 = "",
                        ShipToRemark = "",
                        ShipToShipVia = "",
                        AcCustomerID = customer.Username



                    }
                };



                int itemcount = 0;
                var orderitems = _orderService.GetOrderItems(order.Id);
            foreach (var item in orderitems)

            { var product = _productService.GetProductById(item.ProductId);
                    var newSmartDetail = new Documentdetail
                    {
                        AcCusInvoiceMID = order.CustomOrderNumber,
                        AcStockID = _productService.FormatSku(product, item.AttributesXml),
                        ItemDiscountAmount = item.DiscountAmountInclTax / item.Quantity,
                        ItemUnitPrice = (item.DiscountAmountInclTax/ item.Quantity) + (item.UnitPriceInclTax),
                        ItemGrossTotal = (item.DiscountAmountInclTax + item.UnitPriceInclTax) * item.Quantity,
                        ItemQuantity = item.Quantity,
                        ItemTotalPrice = item.PriceInclTax,
                        AcStockUOMID = "UNIT",
                        ItemRemark1 = product.Name,
                        ItemNo = (++itemcount).ToString()

                    };
                    smartDo.DocumentDetails.Add(newSmartDetail);
            }
                if (order.OrderShippingInclTax > 0)
                {

                    var newSmartDetail = new Documentdetail
                    {
                        AcCusInvoiceMID = order.CustomOrderNumber,
                        AcStockID = "SVC001",
                        ItemDiscountAmount = 0,
                        ItemUnitPrice = order.OrderShippingInclTax,
                        ItemGrossTotal = order.OrderShippingInclTax,
                        ItemQuantity = 1,
                        ItemTotalPrice = order.OrderShippingInclTax,
                        AcStockUOMID = "UNIT",
                        ItemRemark1 = "Shipping",
                        
                        ItemNo = (++itemcount).ToString()

                    };
                    smartDo.DocumentDetails.Add(newSmartDetail);

                }










            return Ok(smartDo);
   
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
          if(_orderProcessingService.CanCancelOrder(order))
                _orderProcessingService.CancelOrder(order, false);
              
                return Ok(new PaymentDetailsDTO { ordernumber = ordernumber, total = _priceFormatter.FormatPrice(order.OrderTotal), mobileno = customer.Username, status = false });
            }
        }


        [HttpGet("api/resendorder")]
        public IActionResult ResendOrder(string ordernumber)
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
                    Note = "Order resent to smart",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });


                var smartDo = new SmartDo
                {
                    DocumentHeader = new Documentheader
                    {
                        AcCusInvoiceMID = order.CustomOrderNumber,
                        AcLocationID = "APP",
                        DocumentDate = "/Date(" + Convert.ToInt64((DateTime.Now.Date - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds) + ")/",
                        DeliveryDate = "/Date(" + Convert.ToInt64((DateTime.Now.AddDays(7).Date - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds) + ")/",
                        DocumentNetAmount = order.OrderTotal,
                        DocumentCentBalance = 0,
                        DocumentFinalAmount = order.OrderTotal,
                        ShipToAddress1 = "",
                        ShipToAddress2 = "",
                        ShipToAddress3 = "",
                        ShipToAddress4 = "",
                        ShipToAttention = "",
                        ShipToContact1 = "",
                        ShipToContact2 = "",
                        DocumentRemark = "",
                        RefDocumentNo = "",
                        DocumentYourReference = "",
                        ExtraRemark1 = "",
                        ExtraRemark2 = "",
                        ExtraRemark3 = "",
                        ExtraRemark4 = "",
                        ShipToFax = "",
                        ShipToPhone1 = "",
                        ShipToPhone2 = "",
                        ShipToRemark = "",
                        ShipToShipVia = "",
                        AcCustomerID = customer.Username



                    }
                };



                int itemcount = 0;
                var orderitems = _orderService.GetOrderItems(order.Id);
                foreach (var item in orderitems)

                {
                    var product = _productService.GetProductById(item.ProductId);
                    var newSmartDetail = new Documentdetail
                    {
                        AcCusInvoiceMID = order.CustomOrderNumber,
                        AcStockID = _productService.FormatSku(product, item.AttributesXml),
                        ItemDiscountAmount = item.DiscountAmountInclTax / item.Quantity,
                        ItemUnitPrice = (item.DiscountAmountInclTax + item.UnitPriceInclTax) / item.Quantity,
                        ItemGrossTotal = (item.DiscountAmountInclTax + item.UnitPriceInclTax) * item.Quantity,
                        ItemQuantity = item.Quantity,
                        ItemTotalPrice = item.PriceInclTax,
                        AcStockUOMID = "UNIT",
                        ItemRemark1 = product.Name,
                        ItemNo = (++itemcount).ToString()

                    };
                    smartDo.DocumentDetails.Add(newSmartDetail);
                }
                if (order.OrderShippingInclTax > 0)
                {

                    var newSmartDetail = new Documentdetail
                    {
                        AcCusInvoiceMID = order.CustomOrderNumber,
                        AcStockID = "SVC001",
                        ItemDiscountAmount = 0,
                        ItemUnitPrice = order.OrderShippingInclTax,
                        ItemGrossTotal = order.OrderShippingInclTax,
                        ItemQuantity = 1,
                        ItemTotalPrice = order.OrderShippingInclTax,
                        AcStockUOMID = "UNIT",
                        ItemRemark1 = "Shipping",

                        ItemNo = (++itemcount).ToString()

                    };
                    smartDo.DocumentDetails.Add(newSmartDetail);

                }










                return Ok(smartDo);

            }


    }
}
         
        
    

