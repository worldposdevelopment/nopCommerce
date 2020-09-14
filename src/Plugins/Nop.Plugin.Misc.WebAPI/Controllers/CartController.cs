using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Misc.WebAPI.Filter;
using Nop.Plugin.Misc.WebAPI.Models;
using Nop.Services.CampaignPromo;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Shipping;
using Nop.Web.Controllers;
using Nop.Web.Factories;
using Nop.Web.Models.Catalog;
using Nop.Web.Models.ShoppingCart;
using StackExchange.Profiling.Internal;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Nop.Plugin.Misc.WebAPI.Controllers
{
    [ApiKeyAuth]
    [Route("")]
    [ApiController]
    public class CartController : ControllerBase
    {

        private readonly ICustomerService _customerService;
        private readonly IShoppingCartService _shoppingCartService;

        private readonly IWorkContext _workContext;
        private readonly IProductService _productService;
        private readonly IDiscountService _discountService;
        private readonly IStoreContext _storeContext;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IShoppingCartModelFactory _shoppingCartModelFactory;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ILocalizationService _localizationService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly PaymentSettings _paymentSettings;
        private readonly IAddressService _addressService;
        private readonly IShippingService _shippingService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IProductModelFactory _productModelFactory;
        private readonly ICheckoutModelFactory _checkoutModelFactory;
        private readonly IPriceFormatter _priceFormatter;
        private readonly ILogger _logger;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly ICampaignPromoService _campaignPromoService;
        public CartController(ICustomerService customerService, IShoppingCartService shoppingCartService, IWorkContext workContext, IProductService productService, IStoreContext storeContext, ShoppingCartSettings shoppingCartSettings, IProductAttributeParser productAttributeParser, IShoppingCartModelFactory shoppingCartModelFactory, ICustomerActivityService customerActivityService, ILocalizationService localizationService, IOrderProcessingService orderProcessingService, PaymentSettings paymentSettings, IShippingService shippingService, IGenericAttributeService genericAttributeService, IAddressService addressService, IProductAttributeService productAttributeService, ILogger logger, IProductModelFactory productModelFactory, ICheckoutModelFactory checkoutModelFactory, IOrderTotalCalculationService orderTotalCalculationService, IPriceFormatter priceFormatter, IPaymentService paymentService, IOrderService orderService, IDiscountService discountService, ICampaignPromoService campaignPromoService)
        {

            _customerService = customerService;
            _shoppingCartService = shoppingCartService;
            _workContext = workContext;
            _productService = productService;
            _storeContext = storeContext;
            _shoppingCartSettings = shoppingCartSettings;
            _productAttributeParser = productAttributeParser;
            _shoppingCartModelFactory = shoppingCartModelFactory;
            _customerActivityService = customerActivityService;
            _localizationService = localizationService;
            _orderProcessingService = orderProcessingService;
            _paymentSettings = paymentSettings;
            _shippingService = shippingService;
            _genericAttributeService = genericAttributeService;
            _addressService = addressService;
            _productAttributeService = productAttributeService;
            _productModelFactory = productModelFactory;
            _logger = logger;
            _checkoutModelFactory = checkoutModelFactory;
            _orderTotalCalculationService = orderTotalCalculationService;
            _priceFormatter = priceFormatter;
            _paymentService = paymentService;
            _orderService = orderService;
            _discountService = discountService;
            _campaignPromoService = campaignPromoService;
        }
        [HttpGet("api/checkcampaignproduct")]
        public IActionResult PickupOptions(int productId, string mobileno, int qty)
        {
            var customer = _customerService.GetCustomerByUsername(mobileno);
            _workContext.CurrentCustomer = customer;
            var product = _productService.GetProductById(productId);
            var valid = _campaignPromoService.ValidPrelaunchPurchase(product, qty);
            if (valid)
                return Ok();
            else
                return BadRequest();


        }
        [HttpGet("api/selectShipping")]
        public IActionResult SelectShipping(string mobileno, int optionid, bool isshipping, int shoppingCartTypeId)
        {
            var customer = _customerService.GetCustomerByUsername(mobileno);
            if (customer == null)
            {
                _logger.Information("Mobile number not registered", null, null);

                return Ok();
            }
            _logger.Information("Selectshipping", null, customer);
            if (shoppingCartTypeId == 0)
                shoppingCartTypeId = 1;
            var shoppingCartType = (ShoppingCartType)shoppingCartTypeId;


            _workContext.CurrentCustomer = customer;
            _logger.Information("Optionid:" + optionid + ";isshipping:" + isshipping, null, customer);

            if (isshipping)
            {
                if (optionid > 0)
                {
                    customer.ShippingAddressId = optionid;
                    customer.BillingAddressId = optionid;
                    _customerService.UpdateCustomer(customer);
                    SetShippingOption("Shipping.FixedByWeightByTotal",
                                                   "DHL",
                                                  _storeContext.CurrentStore.Id,
                                                   customer,
                                                  _shoppingCartService.GetShoppingCart(customer, shoppingCartType).ToList(), _addressService.GetAddressById(optionid));
                    _logger.Information("Address set to :" + optionid, null, customer);
                }

            }
            else
            {
                var pickupPoints = _shippingService.GetPickupPoints(_workContext.CurrentCustomer.BillingAddressId ?? 0,
              _workContext.CurrentCustomer, "Pickup.PickupInStore", _storeContext.CurrentStore.Id).PickupPoints.ToList();
                var selectedPoint = pickupPoints.FirstOrDefault(x => x.Id.Equals(optionid.ToString()));
                SavePickupOption(selectedPoint);

            }
            var cart = _shoppingCartService.GetShoppingCart(customer, shoppingCartType, 1).Where(s => s.SelectedForCheckout == 1).ToList();
            var model = new ShoppingCartModel();
            model = _shoppingCartModelFactory.PrepareShoppingCartModel(model, cart, true, true, true);


            foreach (Nop.Web.Models.ShoppingCart.ShoppingCartModel.ShoppingCartItemModel item in model.Items)
            {
                var product = _productService.GetProductById(item.ProductId);

                var productAttributes = _productAttributeService.GetProductAttributeMappingsByProductId(item.ProductId);
                if (productAttributes != null)
                    item.CustomProperties.Add("ProductAttributes", PrepareProductAttributeModels(product, null));



            }
            model.TotalFee = EstimateFee(shoppingCartType);
            return Ok(model);


        }

        [HttpGet("api/pickupoptions")]
        public IActionResult PickupOptions(string mobileno, int shoppingCartTypeId)
        {
            var customer = _customerService.GetCustomerByUsername(mobileno);
            if (customer == null)
            {
                _logger.Information("Mobile number not registered", null, null);

                return Ok();
            }
            if (shoppingCartTypeId == 0)
                shoppingCartTypeId = 1;
            var shoppingCartType = (ShoppingCartType)shoppingCartTypeId;


            _workContext.CurrentCustomer = customer;
            var cart = _shoppingCartService.GetShoppingCart(customer, shoppingCartType, 1);


            var model = _checkoutModelFactory.PrepareShippingMethodModel(cart, _customerService.GetCustomerShippingAddress(_workContext.CurrentCustomer));
            return Ok(model.PickupPointsModel);

        }



        [HttpGet("api/applydiscountcouponcode")]
        public virtual IActionResult ApplyDiscountCoupon(string discountcouponcode, string mobileno)
        {
            var customer = _customerService.GetCustomerByUsername(mobileno);
            if (customer == null)
            {
                _logger.Information("Mobile number not registered", null, null);

                return Ok();
            }
            _workContext.CurrentCustomer = customer;
            //trim
            if (discountcouponcode != null)
                discountcouponcode = discountcouponcode.Trim();

            //cart
            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id);

            var model = new ShoppingCartModel();
            if (!string.IsNullOrWhiteSpace(discountcouponcode))
            {
                //we find even hidden records here. this way we can display a user-friendly message if it's expired
                var discounts = _discountService.GetAllDiscounts(couponCode: discountcouponcode, showHidden: true)
                    .Where(d => d.RequiresCouponCode)
                    .ToList();
                if (discounts.Any())
                {
                    var userErrors = new List<string>();
                    var anyValidDiscount = discounts.Any(discount =>
                    {
                        var validationResult = _discountService.ValidateDiscount(discount, _workContext.CurrentCustomer, new[] { discountcouponcode });
                        userErrors.AddRange(validationResult.Errors);

                        return validationResult.IsValid;
                    });

                    if (anyValidDiscount)
                    {
                        //valid
                        _customerService.ApplyDiscountCouponCode(_workContext.CurrentCustomer, discountcouponcode);
                        model.DiscountBox.Messages.Add(_localizationService.GetResource("ShoppingCart.DiscountCouponCode.Applied"));
                        model.DiscountBox.IsApplied = true;
                    }
                    else
                    {
                        if (userErrors.Any())
                            //some user errors
                            model.DiscountBox.Messages = userErrors;
                        else
                            //general error text
                            model.DiscountBox.Messages.Add(_localizationService.GetResource("ShoppingCart.DiscountCouponCode.WrongDiscount"));
                    }
                }
                else
                    //discount cannot be found
                    model.DiscountBox.Messages.Add(_localizationService.GetResource("ShoppingCart.DiscountCouponCode.CannotBeFound"));
            }
            else
                //empty coupon code
                model.DiscountBox.Messages.Add(_localizationService.GetResource("ShoppingCart.DiscountCouponCode.Empty"));

            model = _shoppingCartModelFactory.PrepareShoppingCartModel(model, cart);

            model.TotalFee = EstimateFee(ShoppingCartType.ShoppingCart);
            //if (addToCartWarnings.Count > 0)
            //    return BadRequest(addToCartWarnings);


            foreach (Nop.Web.Models.ShoppingCart.ShoppingCartModel.ShoppingCartItemModel item in model.Items)
            {
                var cartproduct = _productService.GetProductById(item.ProductId);

                item.CustomProperties.Add("ProductAttributes", PrepareProductAttributeModels(cartproduct, null));



            }

            _logger.Information("Discount Return" + model.ToJson(), null, null);

            //return result
            return Ok(model);

        }


        [HttpPost("api/wishlist")]
        public IActionResult Wishlist(String mobileno)
        {
            if (String.IsNullOrEmpty(mobileno))
            {
                _logger.Information("Mobile number is null", null, null);
                return Ok();


            }

            try
            {
                var customer = _customerService.GetCustomerByUsername(mobileno);
                if (customer == null)
                {
                    _logger.Information("Mobile number not registered", null, null);

                    return Ok();
                }
                _workContext.CurrentCustomer = customer;
                //if (!_permissionService.Authorize(StandardPermissionProvider.EnableShoppingCart))
                //    return RedirectToRoute("Homepage");

                var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, ShoppingCartType.Wishlist, _storeContext.CurrentStore.Id);
                var model = new ShoppingCartModel();
                model = _shoppingCartModelFactory.PrepareShoppingCartModel(model, cart, true, true, true);
                model.TotalFee = EstimateFee(ShoppingCartType.Wishlist);

                foreach (Nop.Web.Models.ShoppingCart.ShoppingCartModel.ShoppingCartItemModel item in model.Items)
                {
                    var product = _productService.GetProductById(item.ProductId);

                    item.CustomProperties.Add("ProductAttributes", PrepareProductAttributeModels(product, null));



                }
                return Ok(model);
            }
            catch (Exception ex)
            {
                _logger.Information("Cart exception", ex, null);
                return Ok();
            }
        }

        [HttpPost("api/onlinerafflecheckout")]
        public IActionResult OnlineRaffleCreateOrder(string mobileno, int addressid)
        {
            if (mobileno != null)
                _logger.Information(mobileno + ": at checkout", null, null);
            else
                _logger.Information("Null mobile at checkout", null, null);
            try
            {
                var customer = _customerService.GetCustomerByUsername(mobileno);
                _workContext.CurrentCustomer = customer;


                if (addressid > 0)
                {
                    customer.ShippingAddressId = addressid;
                    customer.BillingAddressId = addressid;
                    _customerService.UpdateCustomer(customer);
                }


                var shoppingcart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, ShoppingCartType.OnlineRaffles, _storeContext.CurrentStore.Id);
                _logger.Information("Retrieved cart", null, null);
                var shoppingCartModel = new ShoppingCartModel();
                shoppingCartModel = _shoppingCartModelFactory.PrepareShoppingCartModel(shoppingCartModel, shoppingcart, true, true, true);
                // We doesn't have to check for value because this is done by the order validator.

                Order order = new Order();
                order.OrderGuid = Guid.NewGuid();
                order.CustomerId = customer.Id;
                order.PaymentMethodSystemName = "Payments.Eghl";
                order.StoreId = _storeContext.CurrentStore.Id;
                _logger.Information("Order prep", null, null);
                //  var shippingRequired = false;

                //if (orderDelta.Dto.OrderItems != null)
                //{
                //    var shouldReturnError = AddOrderItemsToCart(orderDelta.Dto.OrderItems, customer, orderDelta.Dto.StoreId ?? _storeContext.CurrentStore.Id);
                //    if (shouldReturnError)
                //    {
                //        return Error(HttpStatusCode.BadRequest);
                //    }
                Boolean isValid = true;
                if (order.PickupInStore == false)
                {
                    isValid = SetShippingOption("Shipping.FixedByWeightByTotal",
                                               "DHL",
                                              _storeContext.CurrentStore.Id,
                                               customer,
                                              _shoppingCartService.GetShoppingCart(customer, ShoppingCartType.OnlineRaffles).ToList(), _customerService.GetCustomerShippingAddress(customer));
                }
                //}

                //if (shippingRequired)
                //{
                //    var isValid = true;



                if (!isValid)
                {
                    return BadRequest();
                }
                //}




                //customer.BillingAddress = newOrder.BillingAddress;
                //customer.ShippingAddress = newOrder.ShippingAddress;

                // If the customer has something in the cart it will be added too. Should we clear the cart first? 
                order.CustomerId = customer.Id;

                // The default value will be the currentStore.id, but if it isn't passed in the json we need to set it by hand.
                //if (!orderDelta.Dto.StoreId.HasValue)
                //{
                //    newOrder.StoreId = _storeContext.CurrentStore.Id;
                //}
                _logger.Information("Before placing order", null, null);
                var placeOrderResult = PlaceOrderOnlineRaffle(order, customer);

                if (!placeOrderResult.Success)
                {
                    foreach (var error in placeOrderResult.Errors)
                    {
                        ModelState.AddModelError("order placement", error);
                    }

                    return BadRequest();
                }

                _customerActivityService.InsertActivity("AddNewOrder",
                    _localizationService.GetResource("ActivityLog.AddNewOrder"), order);
                var postProcessPaymentRequest = new PostProcessPaymentRequest
                {
                    Order = order
                };

                //var ordersRootObject = new OrdersRootObject();

                //var placedOrderDto = _dtoHelper.PrepareOrderDTO(placeOrderResult.PlacedOrder);

                //ordersRootObject.Orders.Add(placedOrderDto);

                //var json = JsonFieldsSerializer.Serialize(ordersRootObject, string.Empty);
                _paymentService.PostProcessPayment(postProcessPaymentRequest);
                string paymenturl = ProcessPaymentRequest();
                //var ordersRootObject = new OrdersRootObject();

                //var placedOrderDto = _dtoHelper.PrepareOrderDTO(placeOrderResult.PlacedOrder);

                //ordersRootObject.Orders.Add(placedOrderDto);

                //var json = JsonFieldsSerializer.Serialize(ordersRootObject, string.Empty);
                shoppingcart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id).ToList();
                _logger.Information("Retrieved cart", null, null);
                shoppingCartModel = new ShoppingCartModel();
                shoppingCartModel = _shoppingCartModelFactory.PrepareShoppingCartModel(shoppingCartModel, shoppingcart, true, true, true);
                foreach (Nop.Web.Models.ShoppingCart.ShoppingCartModel.ShoppingCartItemModel item in shoppingCartModel.Items)
                {
                    var cartproduct = _productService.GetProductById(item.ProductId);

                    item.CustomProperties.Add("ProductAttributes", PrepareProductAttributeModels(cartproduct, null));



                }
                shoppingCartModel.CustomProperties.Add("paymenturl", paymenturl);
                shoppingCartModel.CustomProperties.Add("orderguid", order.OrderGuid);
                return Ok(shoppingCartModel);
            }
            catch (Exception ex)
            {
                _logger.Information("Checkout failed", ex, null);
                return Ok();
            }

        }
        [HttpPost("api/offlinerafflecheckout")]
        public IActionResult OfflineRaffleCreateOrder(string mobileno, int addressid)
        {
            if (mobileno != null)
                _logger.Information(mobileno + ": at checkout", null, null);
            else
                _logger.Information("Null mobile at checkout", null, null);
            try
            {
                var customer = _customerService.GetCustomerByUsername(mobileno);
                _workContext.CurrentCustomer = customer;
                if (customer.BillingAddressId == null)
                {
                    var address = _customerService.GetAddressesByCustomerId(customer.Id).FirstOrDefault();

                    if (address == null)
                        return NotFound();
                    else
                    {
                        customer.BillingAddressId = address.Id;
                        _customerService.UpdateCustomer(customer);

                    }
                }

                if (addressid > 0)
                {
                    var pickupPoints = _shippingService.GetPickupPoints(_workContext.CurrentCustomer.BillingAddressId ?? 0,
                   _workContext.CurrentCustomer, "Pickup.PickupInStore", _storeContext.CurrentStore.Id).PickupPoints.ToList();
                    var selectedPoint = pickupPoints.FirstOrDefault(x => x.Id.Equals(addressid.ToString()));
                    SavePickupOption(selectedPoint);
                }


                var shoppingcart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, ShoppingCartType.OfflineRaffles, _storeContext.CurrentStore.Id);
                _logger.Information("Retrieved cart", null, null);
                var shoppingCartModel = new ShoppingCartModel();
                shoppingCartModel = _shoppingCartModelFactory.PrepareShoppingCartModel(shoppingCartModel, shoppingcart, true, true, true);
                // We doesn't have to check for value because this is done by the order validator.

                Order order = new Order();
                order.OrderGuid = Guid.NewGuid();
                order.CustomerId = customer.Id;
                order.PaymentMethodSystemName = "Payments.Eghl";
                order.StoreId = _storeContext.CurrentStore.Id;
                _logger.Information("Order prep", null, null);
                //  var shippingRequired = false;

                //if (orderDelta.Dto.OrderItems != null)
                //{
                //    var shouldReturnError = AddOrderItemsToCart(orderDelta.Dto.OrderItems, customer, orderDelta.Dto.StoreId ?? _storeContext.CurrentStore.Id);
                //    if (shouldReturnError)
                //    {
                //        return Error(HttpStatusCode.BadRequest);
                //    }
                Boolean isValid = true;
                //if (order.PickupInStore == false)
                //{
                //    isValid = SetShippingOption("Shipping.FixedByWeightByTotal",
                //                               "Hoops Station Shipping",
                //                              _storeContext.CurrentStore.Id,
                //                               customer,
                //                              _shoppingCartService.GetShoppingCart(customer, ShoppingCartType.OfflineRaffles).ToList(), _customerService.GetCustomerShippingAddress(customer));
                //}
                //}

                //if (shippingRequired)
                //{
                //    var isValid = true;



                //if (!isValid)
                //{
                //    return BadRequest();
                //}
                //}




                //customer.BillingAddress = newOrder.BillingAddress;
                //customer.ShippingAddress = newOrder.ShippingAddress;

                // If the customer has something in the cart it will be added too. Should we clear the cart first? 
                order.CustomerId = customer.Id;

                // The default value will be the currentStore.id, but if it isn't passed in the json we need to set it by hand.
                //if (!orderDelta.Dto.StoreId.HasValue)
                //{
                //    newOrder.StoreId = _storeContext.CurrentStore.Id;
                //}
                _logger.Information("Before placing order", null, null);
                var placeOrderResult = PlaceOrderOfflineRaffle(order, customer);

                if (!placeOrderResult.Success)
                {
                    foreach (var error in placeOrderResult.Errors)
                    {
                        ModelState.AddModelError("order placement", error);
                    }

                    return BadRequest();
                }

                _customerActivityService.InsertActivity("AddNewOrder",
                    _localizationService.GetResource("ActivityLog.AddNewOrder"), order);
                var postProcessPaymentRequest = new PostProcessPaymentRequest
                {
                    Order = order
                };

                //var ordersRootObject = new OrdersRootObject();

                //var placedOrderDto = _dtoHelper.PrepareOrderDTO(placeOrderResult.PlacedOrder);

                //ordersRootObject.Orders.Add(placedOrderDto);

                //var json = JsonFieldsSerializer.Serialize(ordersRootObject, string.Empty);
                _paymentService.PostProcessPayment(postProcessPaymentRequest);
                string paymenturl = ProcessPaymentRequest();
                //var ordersRootObject = new OrdersRootObject();

                //var placedOrderDto = _dtoHelper.PrepareOrderDTO(placeOrderResult.PlacedOrder);

                //ordersRootObject.Orders.Add(placedOrderDto);

                //var json = JsonFieldsSerializer.Serialize(ordersRootObject, string.Empty);
                shoppingcart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id).ToList();
                _logger.Information("Retrieved cart", null, null);
                shoppingCartModel = new ShoppingCartModel();
                shoppingCartModel = _shoppingCartModelFactory.PrepareShoppingCartModel(shoppingCartModel, shoppingcart, true, true, true);
                foreach (Nop.Web.Models.ShoppingCart.ShoppingCartModel.ShoppingCartItemModel item in shoppingCartModel.Items)
                {
                    var cartproduct = _productService.GetProductById(item.ProductId);

                    item.CustomProperties.Add("ProductAttributes", PrepareProductAttributeModels(cartproduct, null));



                }
                shoppingCartModel.CustomProperties.Add("paymenturl", paymenturl);
                shoppingCartModel.CustomProperties.Add("orderguid", order.OrderGuid);
                return Ok(shoppingCartModel);
            }
            catch (Exception ex)
            {
                _logger.Information("Checkout failed", ex, null);
                return Ok();
            }

        }
        [HttpPost("api/updatecart")]
        public IActionResult UpdateCart([FromBody] IList<ProductAttributeMobile> products, string mobileno, int shoppingCartTypeId = 1)
        {
            if (shoppingCartTypeId == 0)
                shoppingCartTypeId = 1;
            var addToCartWarnings = new List<string>();
            var customer = _customerService.GetCustomerByUsername(mobileno);

            var shoppingCartType = (ShoppingCartType)shoppingCartTypeId;
            ShoppingCartItem updatecartitem = null;
            _workContext.CurrentCustomer = customer;


            if (String.IsNullOrEmpty(mobileno))
            {
                _logger.Information("Mobile number is null", null, null);
                return Ok();


            }
            _logger.Information(products.ToJson(), null, customer);

            foreach (ProductAttributeMobile item in products)
            {
                var product = _productService.GetProductById(item.ProductId);

                var attributesXml = string.Empty;

                ;
                //product and gift card attributes
                //    var attributes = PaseProductAttribute(productAtrribute);

                if (item.qty < 1)
                    addToCartWarnings.Add(_localizationService.GetResource("Products.QuantityShouldBePositive"));

                if (!String.IsNullOrEmpty(item.ProductAttributeId))
                {
                    var productAttributes = _productAttributeService.GetProductAttributeMappingsByProductId(item.ProductId);
                    var attribute = productAttributes.Where(p => p.ProductId == item.ProductId && p.ProductAttributeId.ToString() == item.ProductAttributeId).FirstOrDefault();
                    attributesXml = AddProductAttribute(attributesXml,
                    attribute, item.ProductAttributeValue.ToString(), item.qty > 1 ? (int?)item.qty : null);

                }

                var cartType = updatecartitem == null ? (ShoppingCartType)shoppingCartTypeId :
                    //if the item to update is found, then we ignore the specified "shoppingCartTypeId" parameter
                    updatecartitem.ShoppingCartType;
                var warnings = _shoppingCartService.GetShoppingCartItemAttributeWarnings(customer, shoppingCartType, product, item.qty, attributesXml);
                var standardwarnings = _shoppingCartService.GetStandardWarnings(customer, shoppingCartType, product, attributesXml, 0, item.qty);
                if (warnings.Count > 0 || standardwarnings.Count > 0)
                    item.HasWarning = true;
                foreach (string warning in warnings)
                {
                    addToCartWarnings.Add(warning);


                }
                foreach (string warning in standardwarnings)
                {
                    addToCartWarnings.Add(warning);


                }
            }
            //if (addToCartWarnings.Count == 0)
            //{ 

            var currentcart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, shoppingCartType, _storeContext.CurrentStore.Id).ToList();
            foreach (ShoppingCartItem item in currentcart)
            {
                _shoppingCartService.DeleteShoppingCartItem(item);
            }
            foreach (ProductAttributeMobile item in products.Where(a => a.HasWarning == false))
            {
                var product = _productService.GetProductById(item.ProductId);

                var attributesXml = string.Empty;
                if (!String.IsNullOrEmpty(item.ProductAttributeId))
                {
                    var productAttributes = _productAttributeService.GetProductAttributeMappingsByProductId(item.ProductId);
                    var attribute = productAttributes.Where(p => p.ProductId == item.ProductId && p.ProductAttributeId.ToString() == item.ProductAttributeId).FirstOrDefault();

                    attributesXml = AddProductAttribute(attributesXml,
                attribute, item.ProductAttributeValue.ToString(), item.qty > 1 ? (int?)item.qty : null);


                }


                //product and gift card attributes
                //    var attributes = PaseProductAttribute(productAtrribute);
                if (item.qty < 1)
                    addToCartWarnings.Add(_localizationService.GetResource("Products.QuantityShouldBePositive"));



                var cartType = updatecartitem == null ? (ShoppingCartType)shoppingCartTypeId :
                    //if the item to update is found, then we ignore the specified "shoppingCartTypeId" parameter
                    updatecartitem.ShoppingCartType;

                SaveItem(null, addToCartWarnings, product, cartType, attributesXml, 0, null, null, item.qty, item.Selected);
            }
            //if (addToCartWarnings.Count > 0)
            //{

            //    return BadRequest(addToCartWarnings);

            //}


            //        };
            var model = new ShoppingCartModel();
            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, shoppingCartType, _storeContext.CurrentStore.Id).ToList();
            model = _shoppingCartModelFactory.PrepareShoppingCartModel(model, cart, true, true, true);


            foreach (Nop.Web.Models.ShoppingCart.ShoppingCartModel.ShoppingCartItemModel item in model.Items)
            {
                var product = _productService.GetProductById(item.ProductId);

                var productAttributes = _productAttributeService.GetProductAttributeMappingsByProductId(item.ProductId);
                if (productAttributes != null)
                    item.CustomProperties.Add("ProductAttributes", PrepareProductAttributeModels(product, null));



            }
            model.TotalFee = EstimateFee(shoppingCartType);
            if (model.Warnings.Count == 0)
                model.Warnings = addToCartWarnings;
            return Ok(model);


        }
        [HttpPost("api/cart")]
        public IActionResult Cart(String mobileno, bool selectedforcheckout)
        {
            if (String.IsNullOrEmpty(mobileno))
            {
                _logger.Information("Mobile number is null", null, null);
                return Ok();


            }

            try
            {
                var customer = _customerService.GetCustomerByUsername(mobileno);
                if (customer == null)
                {
                    _logger.Information("Mobile number not registered", null, null);

                    return Ok();
                }
                _workContext.CurrentCustomer = customer;
                //if (!_permissionService.Authorize(StandardPermissionProvider.EnableShoppingCart))
                //    return RedirectToRoute("Homepage");

                var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id);
                if (selectedforcheckout)
                    cart = cart.Where(s => s.SelectedForCheckout == 1).ToList();
                var model = new ShoppingCartModel();
                model = _shoppingCartModelFactory.PrepareShoppingCartModel(model, cart, true, true, true);
                model.TotalFee = EstimateFee(ShoppingCartType.ShoppingCart);

                foreach (Nop.Web.Models.ShoppingCart.ShoppingCartModel.ShoppingCartItemModel item in model.Items)
                {
                    var product = _productService.GetProductById(item.ProductId);

                    item.CustomProperties.Add("ProductAttributes", PrepareProductAttributeModels(product, null));



                }
                return Ok(model);
            }
            catch (Exception ex)
            {
                _logger.Information("Cart exception", ex, null);
                return Ok();
            }
        }

        [HttpPost("api/addtocart")]
        public IActionResult AddToCart(string mobileno, int shoppingCartTypeId, Boolean buynow, [FromBody]ProductAttributeMobile productAtrribute)
        {

            {

                if (String.IsNullOrEmpty(mobileno))
                {
                    _logger.Information("Mobile number is null", null, null);
                    return Ok();


                }

                List<string> errors = new List<string>();


                var customer = _customerService.GetCustomerByUsername(mobileno);
                if (customer == null)
                {
                    _logger.Information("Mobile number not registered", null, null);
                    _workContext.CurrentCustomer = customer;
                    return Ok();
                }

                _workContext.CurrentCustomer = customer;
           
                if (customer.BillingAddressId == null)
                {
                    _logger.Information("Billing address is null", null, null);
                    var address = _customerService.GetAddressesByCustomerId(customer.Id).FirstOrDefault();

                    if (address == null)
                    {
                        _logger.Information("No address for user", null, null);
                        return NotFound();

                    }


                    else
                    {
                        customer.BillingAddressId = address.Id;
                        _customerService.UpdateCustomer(customer);

                    }
                }
                if (productAtrribute != null)
                    _logger.Information(productAtrribute.ToJson(), null, customer);
                ////migrate shopping cart
                //_shoppingCartService.MigrateShoppingCart(_workContext.CurrentCustomer, customer, true);

                ////sign in new customer
                //_authenticationService.SignIn(customer, model.RememberMe);

                ////raise event       
                //_eventPublisher.Publish(new CustomerLoggedinEvent(customer));

                ////activity log
                //_customerActivityService.InsertActivity(customer, "PublicStore.Login",
                //    _localizationService.GetResource("ActivityLog.PublicStore.Login"), customer);
                if (shoppingCartTypeId < 1)
                    shoppingCartTypeId = 1;
           

                var shoppingCartType = (ShoppingCartType)shoppingCartTypeId;
                if (shoppingCartTypeId == 3 || shoppingCartTypeId == 4)
                {
                    var currentcart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, shoppingCartType, _storeContext.CurrentStore.Id).ToList();
                    foreach (ShoppingCartItem item in currentcart)
                    {
                        _shoppingCartService.DeleteShoppingCartItem(item);
                    }
                }
                var product = _productService.GetProductById(productAtrribute.ProductId);
                if (product == null)
                {
                    //return Json(new
                    //{
                    //    redirect = Url.RouteUrl("Homepage")
                    //});
                }
                if (buynow == true || shoppingCartTypeId == 3 || shoppingCartTypeId == 4)
                    productAtrribute.Selected = true;

                //we can add only simple products
                if (product.ProductType != ProductType.SimpleProduct)
                {
                    //return Json(new
                    //{
                    //    success = false,
                    //    message = "Only simple products could be added to the cart"
                    //});
                }

                ShoppingCartItem updatecartitem = null;
                //if (_shoppingCartSettings.AllowCartItemEditing && updatecartitemid > 0)
                //{
                //    //search with the same cart type as specified
                //    var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, (ShoppingCartType)shoppingCartTypeId, _storeContext.CurrentStore.Id);

                //    updatecartitem = cart.FirstOrDefault(x => x.Id == updatecartitemid);
                //    //not found? let's ignore it. in this case we'll add a new item
                //    //if (updatecartitem == null)
                //    //{
                //    //    return Json(new
                //    //    {
                //    //        success = false,
                //    //        message = "No shopping cart item found to update"
                //    //    });
                //    //}
                //    //is it this product?
                //    if (updatecartitem != null && product.Id != updatecartitem.ProductId)
                //    {
                //        return Json(new
                //        {
                //            success = false,
                //            message = "This product does not match a passed shopping cart item identifier"
                //        });
                //    }
                //}

                var addToCartWarnings = new List<string>();

                //customer entered price
                var customerEnteredPriceConverted = 0;

                //entered quantity
                var quantity = productAtrribute.qty;
                var attributesXml = string.Empty;

                ;
                //product and gift card attributes
                //    var attributes = PaseProductAttribute(productAtrribute);
                if (productAtrribute.qty < 1)
                    addToCartWarnings.Add(_localizationService.GetResource("Products.QuantityShouldBePositive"));
                if (!String.IsNullOrEmpty(productAtrribute.ProductAttributeId) && !String.IsNullOrEmpty(productAtrribute.ProductAttributeValue))
                {
                    var productAttributes = _productAttributeService.GetProductAttributeMappingsByProductId(product.Id);
                    var attribute = productAttributes.Where(p => p.ProductId == productAtrribute.ProductId && p.ProductAttributeId.ToString() == productAtrribute.ProductAttributeId).FirstOrDefault();
                    attributesXml = AddProductAttribute(attributesXml,
                        attribute, productAtrribute.ProductAttributeValue.ToString(), quantity > 1 ? (int?)quantity : null);

                }
                var cartType = updatecartitem == null ? (ShoppingCartType)shoppingCartTypeId :
                    //if the item to update is found, then we ignore the specified "shoppingCartTypeId" parameter
                    updatecartitem.ShoppingCartType;
                var shoppingcart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, shoppingCartType, _storeContext.CurrentStore.Id);
                bool deletefromwishlist = false;
                foreach (ShoppingCartItem sci in shoppingcart)
                {
                    if (buynow)
                        if (sci.ProductId != product.Id)
                            _shoppingCartService.UpdateShoppingCartItem(customer, sci.Id, sci.AttributesXml, 0, null, null, sci.Quantity, true, false);
                    if (shoppingCartTypeId == 2)
                        if (sci.ProductId == product.Id)
                        {
                            _shoppingCartService.DeleteShoppingCartItem(sci, true);
                            deletefromwishlist = true;
                        }
                }
                if (!deletefromwishlist)
                    SaveItem(null, addToCartWarnings, product, cartType, attributesXml, customerEnteredPriceConverted, null, null, quantity, productAtrribute.Selected);

                shoppingcart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, shoppingCartType, _storeContext.CurrentStore.Id);
                var pickupPoints = _shippingService.GetPickupPoints(_workContext.CurrentCustomer.BillingAddressId ?? 0,
      _workContext.CurrentCustomer, "Pickup.PickupInStore", _storeContext.CurrentStore.Id).PickupPoints.ToList();
                var pickupPoint = new PickupPoint();
                if (shoppingCartType == ShoppingCartType.OfflineRaffles)
                {
                    if (shoppingcart.Count > 0)
                    {

                        var offlineRaffleProduct = _productService.GetProductById(shoppingcart.FirstOrDefault().ProductId);
                        if (offlineRaffleProduct != null)
                        {
                            int storeId = _campaignPromoService.GetStorePickupPointOfflineRaffle(offlineRaffleProduct);
                            pickupPoint = pickupPoints.FirstOrDefault(x => x.Id.Equals(storeId.ToString()));
                            SavePickupOption(pickupPoint);
                        }

                    }
                }

                var shoppingCartModel = new ShoppingCartModel();
                shoppingCartModel = _shoppingCartModelFactory.PrepareShoppingCartModel(shoppingCartModel, shoppingcart, true, true, true);

                shoppingCartModel.TotalFee = EstimateFee(cartType);
                //if (addToCartWarnings.Count > 0)
                //    return BadRequest(addToCartWarnings);


                foreach (Nop.Web.Models.ShoppingCart.ShoppingCartModel.ShoppingCartItemModel item in shoppingCartModel.Items)
                {
                    var cartproduct = _productService.GetProductById(item.ProductId);

                    item.CustomProperties.Add("ProductAttributes", PrepareProductAttributeModels(cartproduct, null));



                }

                _logger.Information("AddToCart Return" + shoppingCartModel.ToJson(), null, null);
                shoppingCartModel.Warnings = addToCartWarnings;
                shoppingCartModel.PickupPoint = pickupPoint;
                //return result
                return Ok(shoppingCartModel);

            }

        }
        private ShoppingCartModel.CalculatedFee EstimateFee(ShoppingCartType shoppingCartType)
        {
            var customer = _workContext.CurrentCustomer;

            decimal shippingfee = 0;
            decimal carttotal = 0;
            decimal cartsubtotal = 0;
            decimal discount = 0;
            decimal discountAmount = 0;
            decimal subTotalWithoutDiscount = 0;
            //var pickupPoint = new PickupPoint();
            var selectedcart = new List<ShoppingCartItem>();

            if (shoppingCartType == ShoppingCartType.OfflineRaffles)
            {


            }
            var shoppingcart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, shoppingCartType, _storeContext.CurrentStore.Id);
            var address = _customerService.GetCustomerShippingAddress(customer);

            if (address == null)
                address = _customerService.GetAddressesByCustomerId(customer.Id).FirstOrDefault();
            Boolean isValid = true;
            //   var pickupPoints = _shippingService.GetPickupPoints(_workContext.CurrentCustomer.BillingAddressId ?? 0,
            //_workContext.CurrentCustomer, "Pickup.PickupInStore", _storeContext.CurrentStore.Id).PickupPoints.ToList();

            //if (shoppingCartType == ShoppingCartType.OfflineRaffles)
            //{
            //    if (shoppingcart.Count > 0)
            //    {

            //      var offlineRaffleProduct = _productService.GetProductById(shoppingcart.FirstOrDefault().ProductId);
            //        if (offlineRaffleProduct != null)
            //        { 
            //            int storeId = _campaignPromoService.GetStorePickupPointOfflineRaffle(offlineRaffleProduct);
            //        pickupPoint = pickupPoints.FirstOrDefault(x => x.Id.Equals(storeId.ToString()));
            //        }

            //    }

            //}
            var pickupPoint = _genericAttributeService.GetAttribute<PickupPoint>(customer,
                 NopCustomerDefaults.SelectedPickupPointAttribute, _storeContext.CurrentStore.Id);



            if (shoppingCartType == ShoppingCartType.ShoppingCart)
            {
                selectedcart = shoppingcart.Where(s => s.SelectedForCheckout > 0).ToList();
            }
            else
            {
                selectedcart = shoppingcart.ToList();

            }


            if (pickupPoint != null)
            {
                //SavePickupOption(pickupPoint);

            }
            else
            {
                isValid = SetShippingOption("Shipping.FixedByWeightByTotal",
                                               "DHL",
                                              _storeContext.CurrentStore.Id,
                                               customer,
                                              selectedcart, address);



                if (selectedcart.Count < 1)
                    return new ShoppingCartModel.CalculatedFee { };

                var shippingOptionResponse = _shippingService.GetShippingOptions(selectedcart, address, customer,
                           "Shipping.FixedByWeightByTotal", _storeContext.CurrentStore.Id);

                if (shippingOptionResponse.Success)
                {
                    var shippingOptions = shippingOptionResponse.ShippingOptions.ToList();

                    var shippingOption = shippingOptions
                        .Find(so => !string.IsNullOrEmpty(so.Name) && so.Name.Equals("DHL", StringComparison.InvariantCultureIgnoreCase));

                    //_genericAttributeService.SaveAttribute(customer,
                    //    NopCustomerDefaults.SelectedShippingOptionAttribute,
                    //    shippingOption, _storeContext.CurrentStore.Id);
                    shippingfee = _orderTotalCalculationService.AdjustShippingRate(shippingOption.Rate, selectedcart, out var _, false);



                }
            }

            if (selectedcart.Count > 0)
            {
                carttotal = _orderTotalCalculationService.GetShoppingCartTotal(selectedcart) ?? 0;
                _orderTotalCalculationService.GetShoppingCartSubTotal(selectedcart, false, out discountAmount, out var appliedDiscounts,
    out subTotalWithoutDiscount, out cartsubtotal, out var taxRates);
            }

            //shippingfee = _orderTotalCalculationService.GetShoppingCartAdditionalShippingCharge(shoppingcart);

            //decimal subtotal = _orderTotalCalculationService.GetShoppingCartSubTotal(shoppingcart, true,0,List<T>, 0,0) ?? 0;
            //var checkout =  _checkoutModelFactory.PreparePaymentInfoModel();
            //return result

            return (new ShoppingCartModel.CalculatedFee { Shippingfee = _priceFormatter.FormatPrice(shippingfee), Subtotal = _priceFormatter.FormatPrice(cartsubtotal), Total = _priceFormatter.FormatPrice(carttotal), DiscountAmount = _priceFormatter.FormatPrice(discountAmount), SubTotalWithoutDiscount = _priceFormatter.FormatPrice(subTotalWithoutDiscount) })
            ;


        }
        [HttpGet("api/selectcartitem")]
        public IActionResult SelectCartItem(string mobileno, int shoppingCartItemId, Boolean selected, int shoppingCartId)
        {


            var customer = _customerService.GetCustomerByUsername(mobileno);
            if (customer == null)
            {
                _logger.Information("Mobile number not registered", null, null);
                _workContext.CurrentCustomer = customer;
                return Ok();
            }



            _workContext.CurrentCustomer = customer;
            if (shoppingCartId < 1)
                shoppingCartId = 1;
            var shoppingCartType = (ShoppingCartType)shoppingCartId;
            _logger.Information("Shopping cart type = " + shoppingCartId, null, null);
            _logger.Information("Selected items for checkout: " + customer.Username);
            _shoppingCartService.SetSelectShoppingCartItem(_workContext.CurrentCustomer, shoppingCartItemId, selected, shoppingCartType);
            var shoppingCartModel = new ShoppingCartModel();
            var shoppingcart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, shoppingCartType, _storeContext.CurrentStore.Id);
            shoppingCartModel = _shoppingCartModelFactory.PrepareShoppingCartModel(shoppingCartModel, shoppingcart, true, true, true);

            foreach (var item in shoppingCartModel.Items)
            {
                var product = _productService.GetProductById(item.ProductId);

                var productAttributes = _productAttributeService.GetProductAttributeMappingsByProductId(item.ProductId);
                if (productAttributes != null)
                    item.CustomProperties.Add("ProductAttributes", PrepareProductAttributeModels(product, null));



            }
       

            shoppingCartModel.TotalFee = EstimateFee(shoppingCartType);
            _logger.Information("Returned selected items" + shoppingCartModel.ToJson(), null, null);
            return Ok(shoppingCartModel);

        }

        //protected virtual Nop.Services.Orders.PlaceOrderContainer PreparePlaceOrderDetails(ProcessPaymentRequest processPaymentRequest)
        //{
        //    var details = new PlaceOrderContainer
        //    {
        //        //customer
        //        Customer = _customerService.GetCustomerById(processPaymentRequest.CustomerId)
        //    };
        //    if (details.Customer == null)
        //        throw new ArgumentException("Customer is not set");

        //    //affiliate
        //    var affiliate = _affiliateService.GetAffiliateById(details.Customer.AffiliateId);
        //    if (affiliate != null && affiliate.Active && !affiliate.Deleted)
        //        details.AffiliateId = affiliate.Id;

        //    //check whether customer is guest
        //    if (_customerService.IsGuest(details.Customer) && !_orderSettings.AnonymousCheckoutAllowed)
        //        throw new NopException("Anonymous checkout is not allowed");

        //    //customer currency
        //    var currencyTmp = _currencyService.GetCurrencyById(
        //        _genericAttributeService.GetAttribute<int>(details.Customer, NopCustomerDefaults.CurrencyIdAttribute, processPaymentRequest.StoreId));
        //    var customerCurrency = currencyTmp != null && currencyTmp.Published ? currencyTmp : _workContext.WorkingCurrency;
        //    var primaryStoreCurrency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);
        //    details.CustomerCurrencyCode = customerCurrency.CurrencyCode;
        //    details.CustomerCurrencyRate = customerCurrency.Rate / primaryStoreCurrency.Rate;

        //    //customer language
        //    details.CustomerLanguage = _languageService.GetLanguageById(
        //        _genericAttributeService.GetAttribute<int>(details.Customer, NopCustomerDefaults.LanguageIdAttribute, processPaymentRequest.StoreId));
        //    if (details.CustomerLanguage == null || !details.CustomerLanguage.Published)
        //        details.CustomerLanguage = _workContext.WorkingLanguage;

        //    //billing address
        //    if (details.Customer.BillingAddressId is null)
        //        throw new NopException("Billing address is not provided");

        //    var billingAddress = _customerService.GetCustomerBillingAddress(details.Customer);

        //    if (!CommonHelper.IsValidEmail(billingAddress?.Email))
        //        throw new NopException("Email is not valid");

        //    details.BillingAddress = _addressService.CloneAddress(billingAddress);

        //    if (_countryService.GetCountryByAddress(details.BillingAddress) is Country billingCountry && !billingCountry.AllowsBilling)
        //        throw new NopException($"Country '{billingCountry.Name}' is not allowed for billing");

        //    //checkout attributes
        //    details.CheckoutAttributesXml = _genericAttributeService.GetAttribute<string>(details.Customer, NopCustomerDefaults.CheckoutAttributes, processPaymentRequest.StoreId);
        //    details.CheckoutAttributeDescription = _checkoutAttributeFormatter.FormatAttributes(details.CheckoutAttributesXml, details.Customer);

        //    //load shopping cart
        //    details.Cart = _shoppingCartService.GetShoppingCart(details.Customer, ShoppingCartType.ShoppingCart, processPaymentRequest.StoreId);

        //    if (!details.Cart.Any())
        //        throw new NopException("Cart is empty");

        //    //validate the entire shopping cart
        //    var warnings = _shoppingCartService.GetShoppingCartWarnings(details.Cart, details.CheckoutAttributesXml, true);
        //    if (warnings.Any())
        //        throw new NopException(warnings.Aggregate(string.Empty, (current, next) => $"{current}{next};"));

        //    //validate individual cart items
        //    foreach (var sci in details.Cart)
        //    {
        //        var product = _productService.GetProductById(sci.ProductId);

        //        var sciWarnings = _shoppingCartService.GetShoppingCartItemWarnings(details.Customer,
        //            sci.ShoppingCartType, product, processPaymentRequest.StoreId, sci.AttributesXml,
        //            sci.CustomerEnteredPrice, sci.RentalStartDateUtc, sci.RentalEndDateUtc, sci.Quantity, false, sci.Id);
        //        if (sciWarnings.Any())
        //            throw new NopException(sciWarnings.Aggregate(string.Empty, (current, next) => $"{current}{next};"));
        //    }

        //    //min totals validation
        //    if (!ValidateMinOrderSubtotalAmount(details.Cart))
        //    {
        //        var minOrderSubtotalAmount = _currencyService.ConvertFromPrimaryStoreCurrency(_orderSettings.MinOrderSubtotalAmount, _workContext.WorkingCurrency);
        //        throw new NopException(string.Format(_localizationService.GetResource("Checkout.MinOrderSubtotalAmount"),
        //            _priceFormatter.FormatPrice(minOrderSubtotalAmount, true, false)));
        //    }

        //    if (!ValidateMinOrderTotalAmount(details.Cart))
        //    {
        //        var minOrderTotalAmount = _currencyService.ConvertFromPrimaryStoreCurrency(_orderSettings.MinOrderTotalAmount, _workContext.WorkingCurrency);
        //        throw new NopException(string.Format(_localizationService.GetResource("Checkout.MinOrderTotalAmount"),
        //            _priceFormatter.FormatPrice(minOrderTotalAmount, true, false)));
        //    }

        //    //tax display type
        //    if (_taxSettings.AllowCustomersToSelectTaxDisplayType)
        //        details.CustomerTaxDisplayType = (TaxDisplayType)_genericAttributeService.GetAttribute<int>(details.Customer, NopCustomerDefaults.TaxDisplayTypeIdAttribute, processPaymentRequest.StoreId);
        //    else
        //        details.CustomerTaxDisplayType = _taxSettings.TaxDisplayType;

        //    //sub total (incl tax)
        //    _orderTotalCalculationService.GetShoppingCartSubTotal(details.Cart, true, out var orderSubTotalDiscountAmount, out var orderSubTotalAppliedDiscounts, out var subTotalWithoutDiscountBase, out var _);
        //    details.OrderSubTotalInclTax = subTotalWithoutDiscountBase;
        //    details.OrderSubTotalDiscountInclTax = orderSubTotalDiscountAmount;

        //    //discount history
        //    foreach (var disc in orderSubTotalAppliedDiscounts)
        //        if (!_discountService.ContainsDiscount(details.AppliedDiscounts, disc))
        //            details.AppliedDiscounts.Add(disc);

        //    //sub total (excl tax)
        //    _orderTotalCalculationService.GetShoppingCartSubTotal(details.Cart, false, out orderSubTotalDiscountAmount,
        //        out orderSubTotalAppliedDiscounts, out subTotalWithoutDiscountBase, out _);
        //    details.OrderSubTotalExclTax = subTotalWithoutDiscountBase;
        //    details.OrderSubTotalDiscountExclTax = orderSubTotalDiscountAmount;

        //    //shipping info
        //    if (_shoppingCartService.ShoppingCartRequiresShipping(details.Cart))
        //    {
        //        var pickupPoint = _genericAttributeService.GetAttribute<PickupPoint>(details.Customer,
        //            NopCustomerDefaults.SelectedPickupPointAttribute, processPaymentRequest.StoreId);
        //        if (_shippingSettings.AllowPickupInStore && pickupPoint != null)
        //        {
        //            var country = _countryService.GetCountryByTwoLetterIsoCode(pickupPoint.CountryCode);
        //            var state = _stateProvinceService.GetStateProvinceByAbbreviation(pickupPoint.StateAbbreviation, country?.Id);

        //            details.PickupInStore = true;
        //            details.PickupAddress = new Address
        //            {
        //                Address1 = pickupPoint.Address,
        //                City = pickupPoint.City,
        //                County = pickupPoint.County,
        //                CountryId = country?.Id,
        //                StateProvinceId = state?.Id,
        //                ZipPostalCode = pickupPoint.ZipPostalCode,
        //                CreatedOnUtc = DateTime.UtcNow
        //            };
        //        }
        //        else
        //        {
        //            if (details.Customer.ShippingAddressId == null)
        //                throw new NopException("Shipping address is not provided");

        //            var shippingAddress = _customerService.GetCustomerShippingAddress(details.Customer);

        //            if (!CommonHelper.IsValidEmail(shippingAddress?.Email))
        //                throw new NopException("Email is not valid");

        //            //clone shipping address
        //            details.ShippingAddress = _addressService.CloneAddress(shippingAddress);

        //            if (_countryService.GetCountryByAddress(details.ShippingAddress) is Country shippingCountry && !shippingCountry.AllowsShipping)
        //                throw new NopException($"Country '{shippingCountry.Name}' is not allowed for shipping");
        //        }

        //        var shippingOption = _genericAttributeService.GetAttribute<ShippingOption>(details.Customer,
        //            NopCustomerDefaults.SelectedShippingOptionAttribute, processPaymentRequest.StoreId);
        //        if (shippingOption != null)
        //        {
        //            details.ShippingMethodName = shippingOption.Name;
        //            details.ShippingRateComputationMethodSystemName = shippingOption.ShippingRateComputationMethodSystemName;
        //        }

        //        details.ShippingStatus = ShippingStatus.NotYetShipped;
        //    }
        //    else
        //        details.ShippingStatus = ShippingStatus.ShippingNotRequired;

        //    //LoadAllShippingRateComputationMethods
        //    var shippingRateComputationMethods = _shippingPluginManager.LoadActivePlugins(_workContext.CurrentCustomer, _storeContext.CurrentStore.Id);

        //    //shipping total
        //    var orderShippingTotalInclTax = _orderTotalCalculationService.GetShoppingCartShippingTotal(details.Cart, true, shippingRateComputationMethods, out var _, out var shippingTotalDiscounts);
        //    var orderShippingTotalExclTax = _orderTotalCalculationService.GetShoppingCartShippingTotal(details.Cart, false, shippingRateComputationMethods);
        //    if (!orderShippingTotalInclTax.HasValue || !orderShippingTotalExclTax.HasValue)
        //        throw new NopException("Shipping total couldn't be calculated");

        //    details.OrderShippingTotalInclTax = orderShippingTotalInclTax.Value;
        //    details.OrderShippingTotalExclTax = orderShippingTotalExclTax.Value;

        //    foreach (var disc in shippingTotalDiscounts)
        //        if (!_discountService.ContainsDiscount(details.AppliedDiscounts, disc))
        //            details.AppliedDiscounts.Add(disc);

        //    //payment total
        //    var paymentAdditionalFee = _paymentService.GetAdditionalHandlingFee(details.Cart, processPaymentRequest.PaymentMethodSystemName);
        //    details.PaymentAdditionalFeeInclTax = _taxService.GetPaymentMethodAdditionalFee(paymentAdditionalFee, true, details.Customer);
        //    details.PaymentAdditionalFeeExclTax = _taxService.GetPaymentMethodAdditionalFee(paymentAdditionalFee, false, details.Customer);

        //    //tax amount
        //    details.OrderTaxTotal = _orderTotalCalculationService.GetTaxTotal(details.Cart, shippingRateComputationMethods, out var taxRatesDictionary);

        //    //VAT number
        //    var customerVatStatus = (VatNumberStatus)_genericAttributeService.GetAttribute<int>(details.Customer, NopCustomerDefaults.VatNumberStatusIdAttribute);
        //    if (_taxSettings.EuVatEnabled && customerVatStatus == VatNumberStatus.Valid)
        //        details.VatNumber = _genericAttributeService.GetAttribute<string>(details.Customer, NopCustomerDefaults.VatNumberAttribute);

        //    //tax rates
        //    details.TaxRates = taxRatesDictionary.Aggregate(string.Empty, (current, next) =>
        //        $"{current}{next.Key.ToString(CultureInfo.InvariantCulture)}:{next.Value.ToString(CultureInfo.InvariantCulture)};   ");

        //    //order total (and applied discounts, gift cards, reward points)
        //    var orderTotal = _orderTotalCalculationService.GetShoppingCartTotal(details.Cart, out var orderDiscountAmount, out var orderAppliedDiscounts, out var appliedGiftCards, out var redeemedRewardPoints, out var redeemedRewardPointsAmount);
        //    if (!orderTotal.HasValue)
        //        throw new NopException("Order total couldn't be calculated");

        //    details.OrderDiscountAmount = orderDiscountAmount;
        //    details.RedeemedRewardPoints = redeemedRewardPoints;
        //    details.RedeemedRewardPointsAmount = redeemedRewardPointsAmount;
        //    details.AppliedGiftCards = appliedGiftCards;
        //    details.OrderTotal = orderTotal.Value;

        //    //discount history
        //    foreach (var disc in orderAppliedDiscounts)
        //        if (!_discountService.ContainsDiscount(details.AppliedDiscounts, disc))
        //            details.AppliedDiscounts.Add(disc);

        //    processPaymentRequest.OrderTotal = details.OrderTotal;

        //    //recurring or standard shopping cart?
        //    details.IsRecurringShoppingCart = _shoppingCartService.ShoppingCartIsRecurring(details.Cart);
        //    if (!details.IsRecurringShoppingCart)
        //        return details;

        //    var recurringCyclesError = _shoppingCartService.GetRecurringCycleInfo(details.Cart,
        //        out var recurringCycleLength, out var recurringCyclePeriod, out var recurringTotalCycles);
        //    if (!string.IsNullOrEmpty(recurringCyclesError))
        //        throw new NopException(recurringCyclesError);

        //    processPaymentRequest.RecurringCycleLength = recurringCycleLength;
        //    processPaymentRequest.RecurringCyclePeriod = recurringCyclePeriod;
        //    processPaymentRequest.RecurringTotalCycles = recurringTotalCycles;

        //    return details;
        //}
        [HttpPost("api/checkout")]
        public IActionResult CreateOrder(string mobileno, int addressid)
        {
            if (mobileno != null)
                _logger.Information(mobileno + ": at checkout", null, null);
            else
                _logger.Information("Null mobile at checkout", null, null);
            try
            {
                var customer = _customerService.GetCustomerByUsername(mobileno);
                _workContext.CurrentCustomer = customer;


                if (addressid > 0)
                {
                    customer.ShippingAddressId = addressid;
                    customer.BillingAddressId = addressid;
                    _customerService.UpdateCustomer(customer);
                }


                var shoppingcart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id).Where(s => s.SelectedForCheckout > 0).ToList();
                _logger.Information("Retrieved cart", null, null);
                var shoppingCartModel = new ShoppingCartModel();
                shoppingCartModel = _shoppingCartModelFactory.PrepareShoppingCartModel(shoppingCartModel, shoppingcart, true, true, true);
                // We doesn't have to check for value because this is done by the order validator.

                Order order = new Order();
                order.OrderGuid = Guid.NewGuid();
                order.CustomerId = customer.Id;
                order.PaymentMethodSystemName = "Payments.Eghl";
                order.StoreId = _storeContext.CurrentStore.Id;
                if (addressid == 0)
                    order.PickupInStore = true;

                _logger.Information("Order prep", null, null);
                //  var shippingRequired = false;

                //if (orderDelta.Dto.OrderItems != null)
                //{
                //    var shouldReturnError = AddOrderItemsToCart(orderDelta.Dto.OrderItems, customer, orderDelta.Dto.StoreId ?? _storeContext.CurrentStore.Id);
                //    if (shouldReturnError)
                //    {
                //        return Error(HttpStatusCode.BadRequest);
                //    }
                Boolean isValid = true;
                if (order.PickupInStore == false)
                {
                    isValid = SetShippingOption("Shipping.FixedByWeightByTotal",
                                               "DHL",
                                              _storeContext.CurrentStore.Id,
                                               customer,
                                              shoppingcart, _customerService.GetCustomerShippingAddress(customer));


                }
                //else
                //{

                //    isValid = SetShippingOption("Shipping.FixedByWeightByTotal",
                //                                  "Hoops Station Shipping",
                //                                 _storeContext.CurrentStore.Id,
                //                                  customer,
                //                                 shoppingcart.Where(s => s.SelectedForCheckout > 0).ToList(), _customerService.GetCustomerShippingAddress(customer));
                //}
                //}

                //if (shippingRequired)
                //{
                //    var isValid = true;



                if (!isValid)
                {
                    return BadRequest();
                }
                //}




                //customer.BillingAddress = newOrder.BillingAddress;
                //customer.ShippingAddress = newOrder.ShippingAddress;

                // If the customer has something in the cart it will be added too. Should we clear the cart first? 
                order.CustomerId = customer.Id;

                // The default value will be the currentStore.id, but if it isn't passed in the json we need to set it by hand.
                //if (!orderDelta.Dto.StoreId.HasValue)
                //{
                //    newOrder.StoreId = _storeContext.CurrentStore.Id;
                //}
                _logger.Information("Before placing order", null, null);
                var placeOrderResult = PlaceOrder(order, customer);

                if (!placeOrderResult.Success)
                {
                    foreach (var error in placeOrderResult.Errors)
                    {
                        ModelState.AddModelError("order placement", error);
                    }

                    return BadRequest();
                }

                _customerActivityService.InsertActivity("AddNewOrder",
                    _localizationService.GetResource("ActivityLog.AddNewOrder"), order);
                var postProcessPaymentRequest = new PostProcessPaymentRequest
                {
                    Order = order
                };
                _paymentService.PostProcessPayment(postProcessPaymentRequest);
                string paymenturl = ProcessPaymentRequest();
                //var ordersRootObject = new OrdersRootObject();

                //var placedOrderDto = _dtoHelper.PrepareOrderDTO(placeOrderResult.PlacedOrder);

                //ordersRootObject.Orders.Add(placedOrderDto);

                //var json = JsonFieldsSerializer.Serialize(ordersRootObject, string.Empty);
                shoppingcart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id).ToList();
                _logger.Information("Retrieved cart", null, null);
                shoppingCartModel = new ShoppingCartModel();
                shoppingCartModel = _shoppingCartModelFactory.PrepareShoppingCartModel(shoppingCartModel, shoppingcart, true, true, true);
                foreach (Nop.Web.Models.ShoppingCart.ShoppingCartModel.ShoppingCartItemModel item in shoppingCartModel.Items)
                {
                    var cartproduct = _productService.GetProductById(item.ProductId);

                    item.CustomProperties.Add("ProductAttributes", PrepareProductAttributeModels(cartproduct, null));



                }
                shoppingCartModel.CustomProperties.Add("paymenturl", paymenturl);
                shoppingCartModel.CustomProperties.Add("orderguid", order.OrderGuid);
                return Ok(shoppingCartModel);
            }
            catch (Exception ex)
            {
                _logger.Information("Checkout failed", ex, null);
                return Ok();
            }

        }
        private PlaceOrderResult PlaceOrder(Order newOrder, Customer customer)
        {
            var processPaymentRequest = new ProcessPaymentRequest
            {
                StoreId = newOrder.StoreId,
                CustomerId = customer.Id,
                PaymentMethodSystemName = newOrder.PaymentMethodSystemName,
                OrderGuid = newOrder.OrderGuid,
                OrderGuidGeneratedOnUtc = DateTime.Now

            };

            GenerateOrderGuid(processPaymentRequest);
            var placeOrderResult = _orderProcessingService.PlaceOrder(processPaymentRequest);

            return placeOrderResult;
        }
        private PlaceOrderResult PlaceOrderOnlineRaffle(Order newOrder, Customer customer)
        {
            var processPaymentRequest = new ProcessPaymentRequest
            {
                StoreId = newOrder.StoreId,
                CustomerId = customer.Id,
                PaymentMethodSystemName = newOrder.PaymentMethodSystemName,
                OrderGuid = newOrder.OrderGuid,
                OrderGuidGeneratedOnUtc = DateTime.Now
            };

            GenerateOrderGuid(processPaymentRequest);
            var placeOrderResult = _orderProcessingService.PlaceOnlineRaffleOrder(processPaymentRequest);

            return placeOrderResult;
        }
        private PlaceOrderResult PlaceOrderOfflineRaffle(Order newOrder, Customer customer)
        {
            var processPaymentRequest = new ProcessPaymentRequest
            {
                StoreId = newOrder.StoreId,
                CustomerId = customer.Id,
                PaymentMethodSystemName = newOrder.PaymentMethodSystemName,
                OrderGuid = newOrder.OrderGuid,
                OrderGuidGeneratedOnUtc = DateTime.Now
            };

            GenerateOrderGuid(processPaymentRequest);
            var placeOrderResult = _orderProcessingService.PlaceOfflineRaffleOrder(processPaymentRequest);

            return placeOrderResult;
        }
        private string ProcessPaymentRequest()
        {
            var order = _orderService.SearchOrders(storeId: _storeContext.CurrentStore.Id, customerId: _workContext.CurrentCustomer.Id, pageSize: 1).FirstOrDefault();

            //That's why we process it here
            var postProcessPaymentRequest = new PostProcessPaymentRequest
            {
                Order = order
            };
            var baseUrl = "https://hoopsmarketing.worldpos.com.my/processpayghl.aspx?";

            //create common query parameters for the request
            var queryParameters = CreateQueryParameters(postProcessPaymentRequest);


            //add order items query parameters to the request
            var parameters = new Dictionary<string, string>(queryParameters);

            var redirectUrl = QueryHelpers.AddQueryString(baseUrl, parameters);
            return redirectUrl;

        }
        protected virtual string PaseProductAttribute(ProductAttributeMobile productAtrribute)
        {
            string xml = "<Attributes><ProductAttribute ID=\"" + productAtrribute.ProductAttributeId + "\"><ProductAttributeValue><Value>" + productAtrribute.ProductAttributeValue + "</Value></ProductAttributeValue></ProductAttribute></Attributes>";
            return xml;

        }
        protected virtual void SaveItem(ShoppingCartItem updatecartitem, List<string> addToCartWarnings, Product product,
    ShoppingCartType cartType, string attributes, decimal customerEnteredPriceConverted, DateTime? rentalStartDate,
    DateTime? rentalEndDate, int quantity, Boolean selected = false)
        {
            if (updatecartitem == null)
            {
                //add to the cart
                addToCartWarnings.AddRange(_shoppingCartService.AddToCart(_workContext.CurrentCustomer,
                    product, cartType, _storeContext.CurrentStore.Id,
                    attributes, customerEnteredPriceConverted,
                    rentalStartDate, rentalEndDate, quantity, true, selected));
            }
            else
            {
                var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, updatecartitem.ShoppingCartType, _storeContext.CurrentStore.Id);

                var otherCartItemWithSameParameters = _shoppingCartService.FindShoppingCartItemInTheCart(
                    cart, updatecartitem.ShoppingCartType, product, attributes, customerEnteredPriceConverted,
                    rentalStartDate, rentalEndDate);
                if (otherCartItemWithSameParameters != null &&
                    otherCartItemWithSameParameters.Id == updatecartitem.Id)
                {
                    //ensure it's some other shopping cart item
                    otherCartItemWithSameParameters = null;
                }
                //update existing item
                addToCartWarnings.AddRange(_shoppingCartService.UpdateShoppingCartItem(_workContext.CurrentCustomer,
                    updatecartitem.Id, attributes, customerEnteredPriceConverted,
                    rentalStartDate, rentalEndDate, quantity + (otherCartItemWithSameParameters?.Quantity ?? 0), true));
                if (otherCartItemWithSameParameters != null && !addToCartWarnings.Any())
                {
                    //delete the same shopping cart item (the other one)
                    _shoppingCartService.DeleteShoppingCartItem(otherCartItemWithSameParameters);
                }
            }
        }
        protected virtual void GenerateOrderGuid(ProcessPaymentRequest processPaymentRequest)
        {

            if (processPaymentRequest.OrderGuid == Guid.Empty)
            {
                processPaymentRequest.OrderGuid = Guid.NewGuid();
                processPaymentRequest.OrderGuidGeneratedOnUtc = DateTime.UtcNow;
            }
        }
        private bool SetShippingOption(string shippingRateComputationMethodSystemName, string shippingOptionName, int storeId, Customer customer, List<ShoppingCartItem> shoppingCartItems, Address address)
        {

            var isValid = true;
            _genericAttributeService.SaveAttribute<ShippingOption>(_workContext.CurrentCustomer, NopCustomerDefaults.SelectedShippingOptionAttribute, null, _storeContext.CurrentStore.Id);
            _genericAttributeService.SaveAttribute<PickupPoint>(_workContext.CurrentCustomer, NopCustomerDefaults.SelectedPickupPointAttribute, null, _storeContext.CurrentStore.Id);

            if (string.IsNullOrEmpty(shippingRateComputationMethodSystemName))
            {
                isValid = false;

                ModelState.AddModelError("shipping_rate_computation_method_system_name",
                    "Please provide shipping_rate_computation_method_system_name");
            }
            else if (string.IsNullOrEmpty(shippingOptionName))
            {
                isValid = false;

                ModelState.AddModelError("shipping_option_name", "Please provide shipping_option_name");
            }
            else
            {
                var shippingOptionResponse = _shippingService.GetShippingOptions(shoppingCartItems, address, customer,
                        shippingRateComputationMethodSystemName, storeId);

                if (shippingOptionResponse.Success)
                {
                    var shippingOptions = shippingOptionResponse.ShippingOptions.ToList();

                    var shippingOption = shippingOptions
                        .Find(so => !string.IsNullOrEmpty(so.Name) && so.Name.Equals(shippingOptionName, StringComparison.InvariantCultureIgnoreCase));

                    _genericAttributeService.SaveAttribute(customer,
                        NopCustomerDefaults.SelectedShippingOptionAttribute,
                        shippingOption, storeId);
                }
                else
                {
                    isValid = false;

                    foreach (var errorMessage in shippingOptionResponse.Errors)
                    {
                        ModelState.AddModelError("shipping_option", errorMessage);
                    }
                }
            }

            return isValid;
        }
        protected virtual void SavePickupOption(PickupPoint pickupPoint)
        {
            _genericAttributeService.SaveAttribute<ShippingOption>(_workContext.CurrentCustomer, NopCustomerDefaults.SelectedShippingOptionAttribute, null, _storeContext.CurrentStore.Id);
            _genericAttributeService.SaveAttribute<PickupPoint>(_workContext.CurrentCustomer, NopCustomerDefaults.SelectedPickupPointAttribute, null, _storeContext.CurrentStore.Id);

            var pickUpInStoreShippingOption = new ShippingOption
            {
                Name = string.Format(_localizationService.GetResource("Checkout.PickupPoints.Name"), pickupPoint.Name),
                Rate = pickupPoint.PickupFee,
                Description = pickupPoint.Description,
                ShippingRateComputationMethodSystemName = pickupPoint.ProviderSystemName,
                IsPickupInStore = true
            };
            _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer, NopCustomerDefaults.SelectedShippingOptionAttribute, pickUpInStoreShippingOption, _storeContext.CurrentStore.Id);
            _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer, NopCustomerDefaults.SelectedPickupPointAttribute, pickupPoint, _storeContext.CurrentStore.Id);
        }


        private string AddProductAttribute(string attributesXml, ProductAttributeMapping productAttributeMapping, string value, int? quantity = null)
        {
            var result = string.Empty;
            try
            {
                var xmlDoc = new XmlDocument();
                if (string.IsNullOrEmpty(attributesXml))
                {
                    var element1 = xmlDoc.CreateElement("Attributes");
                    xmlDoc.AppendChild(element1);
                }
                else
                {
                    xmlDoc.LoadXml(attributesXml);
                }

                var rootElement = (XmlElement)xmlDoc.SelectSingleNode(@"//Attributes");

                XmlElement attributeElement = null;
                //find existing
                var nodeList1 = xmlDoc.SelectNodes(@"//Attributes/ProductAttribute");
                foreach (XmlNode node1 in nodeList1)
                {
                    if (node1.Attributes?["ID"] == null)
                        continue;

                    var str1 = node1.Attributes["ID"].InnerText.Trim();
                    if (!int.TryParse(str1, out var id))
                        continue;

                    if (id != productAttributeMapping.Id)
                        continue;

                    attributeElement = (XmlElement)node1;
                    break;
                }

                //create new one if not found
                if (attributeElement == null)
                {
                    attributeElement = xmlDoc.CreateElement("ProductAttribute");
                    attributeElement.SetAttribute("ID", productAttributeMapping.Id.ToString());
                    rootElement.AppendChild(attributeElement);
                }

                var attributeValueElement = xmlDoc.CreateElement("ProductAttributeValue");
                attributeElement.AppendChild(attributeValueElement);

                var attributeValueValueElement = xmlDoc.CreateElement("Value");
                attributeValueValueElement.InnerText = value;
                attributeValueElement.AppendChild(attributeValueValueElement);

                //the quantity entered by the customer
                if (quantity.HasValue)
                {
                    var attributeValueQuantity = xmlDoc.CreateElement("Quantity");
                    attributeValueQuantity.InnerText = quantity.ToString();
                    attributeValueElement.AppendChild(attributeValueQuantity);
                }

                result = xmlDoc.OuterXml;
            }
            catch (Exception exc)
            {
                //Debug.Write(exc.ToString());
            }

            return result;
        }
        protected virtual IList<ProductDetailsModel.ProductAttributeModel> PrepareProductAttributeModels(Product product, ShoppingCartItem updatecartitem)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var model = new List<ProductDetailsModel.ProductAttributeModel>();
            _logger.Information(model.ToJson(), null, null);

            var productAttributeMapping = _productAttributeService.GetProductAttributeMappingsByProductId(product.Id);
            _logger.Information(productAttributeMapping.ToJson(), null, null);
            foreach (var attribute in productAttributeMapping)
            {
                var productAttrubute = _productAttributeService.GetProductAttributeById(attribute.ProductAttributeId);
                _logger.Information("ProductAttribute:" + productAttrubute.ToJson(), null, null);
                var attributeModel = new ProductDetailsModel.ProductAttributeModel
                {
                    Id = attribute.Id,
                    ProductId = product.Id,
                    ProductAttributeId = attribute.ProductAttributeId,
                    Name = _localizationService.GetLocalized(productAttrubute, x => x.Name),
                    Description = _localizationService.GetLocalized(productAttrubute, x => x.Description),
                    TextPrompt = _localizationService.GetLocalized(attribute, x => x.TextPrompt),
                    IsRequired = attribute.IsRequired,
                    AttributeControlType = attribute.AttributeControlType,
                    DefaultValue = updatecartitem != null ? null : _localizationService.GetLocalized(attribute, x => x.DefaultValue),
                    HasCondition = !string.IsNullOrEmpty(attribute.ConditionAttributeXml)
                };
                if (!string.IsNullOrEmpty(attribute.ValidationFileAllowedExtensions))
                {
                    attributeModel.AllowedFileExtensions = attribute.ValidationFileAllowedExtensions
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .ToList();
                }

                if (attribute.ShouldHaveValues())
                {
                    //values
                    var attributeValues = _productAttributeService.GetProductAttributeValues(attribute.Id);
                    foreach (var attributeValue in attributeValues)
                    {
                        var valueModel = new ProductDetailsModel.ProductAttributeValueModel
                        {
                            Id = attributeValue.Id,
                            Name = _localizationService.GetLocalized(attributeValue, x => x.Name),
                            ColorSquaresRgb = attributeValue.ColorSquaresRgb, //used with "Color squares" attribute type
                            IsPreSelected = attributeValue.IsPreSelected,
                            CustomerEntersQty = attributeValue.CustomerEntersQty,
                            Quantity = attributeValue.Quantity
                        };
                        attributeModel.Values.Add(valueModel);

                        //display price if allowed
                    }
                }
                _logger.Information("Attributemodel:" + attributeModel.ToJson(), null, null);
                model.Add(attributeModel);
            }

            return model;
        }
        private IDictionary<string, string> CreateQueryParameters(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            //get store location
            //var storeLocation = _webHelper.GetStoreLocation();

            //choosing correct order address
            var orderAddress = _addressService.GetAddressById(
                (postProcessPaymentRequest.Order.PickupInStore ? postProcessPaymentRequest.Order.PickupAddressId : postProcessPaymentRequest.Order.ShippingAddressId) ?? 0);
            var roundedOrderTotal = Math.Round(postProcessPaymentRequest.Order.OrderTotal, 2);
            var amount = roundedOrderTotal.ToString("0.00", CultureInfo.InvariantCulture);
            //create query parameters
            var hash = ComputeSha256Hash("wJK5prwuHSS" + postProcessPaymentRequest.Order.CustomOrderNumber + "https://hoopsmarketing.worldpos.com.my/paystatusghl"+ "https://hoopsmarketing.worldpos.com.my/paystatusghl" + amount + postProcessPaymentRequest.Order.CustomerCurrencyCode + postProcessPaymentRequest.Order.CustomerIp);
            var customer = _workContext.CurrentCustomer;
            var address = _customerService.GetAddressesByCustomerId(customer.Id).FirstOrDefault();
            return new Dictionary<string, string>
            {

                ["TransactionType"] = "Sale",


                ["PymtMethod"] = "Any",

                ["ServiceID"] = "HSS",

                ["PaymentID"] = postProcessPaymentRequest.Order.CustomOrderNumber,
                ["OrderNumber"] = postProcessPaymentRequest.Order.CustomOrderNumber,
                //PDT, IPN and cancel URL
                ["PaymentDesc"] = "Hoopsstation app purchase",
                ["MerchantReturnURL"] = "https://hoopsmarketing.worldpos.com.my/paystatusghl",
                ["MerchantCallbackURL"] = "https://hoopsmarketing.worldpos.com.my/paystatusghl",
                ["Amount"] = amount,

                ["CustIP"] = postProcessPaymentRequest.Order.CustomerIp + "",
                //shipping address, if exists
                ["CurrencyCode"] = postProcessPaymentRequest.Order.CustomerCurrencyCode + "",
                ["HashValue"] = hash,
                ["CustName"] = (address.FirstName + " " + address.LastName),
                ["CustEmail"] = customer.Email + "",
                ["CustPhone"] = customer.Username + ""

            };
        }

        static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

    }

}
