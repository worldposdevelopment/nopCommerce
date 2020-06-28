using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Plugin.Misc.WebAPI.Filter;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Logging;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Extensions;
using Nop.Web.Factories;
using Nop.Web.Framework.Controllers;
using Nop.Web.Models.Customer;
using StackExchange.Profiling.Internal;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Nop.Plugin.Misc.WebAPI.Controllers
{
    [ApiKeyAuth]
    [Route("")]
    [ApiController]
    public class AddressController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly IWorkContext _workContext;
        private readonly Web.Factories.ICustomerModelFactory _customerModelFactory;
        private readonly IAddressService _addressService;
        private readonly IAddressAttributeParser _addressAttributeParser;
        private readonly IAddressModelFactory _addressModelFactory;
        private readonly AddressSettings _addressSettings;
        private readonly ICountryService _countryService;
        private readonly ILogger _logger;
        // GET: /<controller>/
        public AddressController(  ICustomerService customerService,

     IWorkContext workContext,
        Web.Factories.ICustomerModelFactory customerModelFactory,
        IAddressService addressService,
     IAddressAttributeParser addressAttributeParser,
     IAddressModelFactory addressModelFactory,
       AddressSettings addressSettings,
       ICountryService countryService, ILogger logger)
        {
            _customerService = customerService;
            _workContext = workContext;
            _customerModelFactory = customerModelFactory;
            _addressService = addressService;
            _addressAttributeParser = addressAttributeParser;
            _addressModelFactory = addressModelFactory;
            _addressSettings = addressSettings;
            _countryService = countryService;
            _logger = logger;

        }
        [HttpGet("api/address/getall")]
        public virtual IActionResult Addresses(string mobileno)
        {
            var customer = _customerService.GetCustomerByUsername(mobileno);
            _workContext.CurrentCustomer = customer;

            if (!_customerService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            var model = _customerModelFactory.PrepareCustomerAddressListModel();
            return Ok(model);
        }

        [HttpPost("api/address/delete")]

        public virtual IActionResult AddressDelete(int addressId, string mobileno)
        {
            var customer = _customerService.GetCustomerByUsername(mobileno);
            _workContext.CurrentCustomer = customer;

            if (!_customerService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

           


            //find address (ensure that it belongs to the current customer)
            var address = _customerService.GetCustomerAddress(customer.Id, addressId);
            if (address != null)
            {
                _customerService.RemoveCustomerAddress(customer, address);
                _customerService.UpdateCustomer(customer);
                //now delete the address record
                _addressService.DeleteAddress(address);
            }

            //redirect to the address list page
            var model = _customerModelFactory.PrepareCustomerAddressListModel();
            return Ok(model);
        }

        //[HttpPost("api/address/add")]
        //public virtual IActionResult AddressAdd(string mobileno)
        //{
        //    var customer = _customerService.GetCustomerByUsername(mobileno);
        //    _workContext.CurrentCustomer = customer;

        //    if (!_customerService.IsRegistered(_workContext.CurrentCustomer))
        //        return Challenge();

        //    var model = new CustomerAddressEditModel();
        //    _addressModelFactory.PrepareAddressModel(model.Address,
        //        address: null,
        //        excludeProperties: false,
        //        addressSettings: _addressSettings,
        //        loadCountries: () => _countryService.GetAllCountries(_workContext.WorkingLanguage.Id));

        //    return Ok(model);
        //}
        [HttpPost("api/address/add")]
           public virtual IActionResult AddressAdd([FromBody]Address address, string mobileno)
        {
            //_logger.Information(address.ToJson(), null, null);
            var customer = _customerService.GetCustomerByUsername(mobileno);
            _workContext.CurrentCustomer = customer;
       

            if (!_customerService.IsRegistered(_workContext.CurrentCustomer))
                return BadRequest();

            //custom address attributes
           // var customAttributes = _addressAttributeParser.ParseCustomAddressAttributes(form);
           // var customAttributeWarnings = _addressAttributeParser.GetAttributeWarnings(customAttributes);
            //foreach (var error in customAttributeWarnings)
            //{
            //    ModelState.AddModelError("", error);
            //}

            //if (ModelState.IsValid)
            //{
                
              //  address.CustomAttributes = customAttributes;
                address.CreatedOnUtc = DateTime.UtcNow;
            //some validation
            if (address.CountryId == 0 || address.CountryId == null)
                address.CountryId = 131;
            if (address.StateProvinceId == 0)
                    address.StateProvinceId = null;


                _addressService.InsertAddress(address);

                _customerService.InsertCustomerAddress(_workContext.CurrentCustomer, address);
            //var addresses = _customerService.GetAddressesByCustomerId(customer.Id);
            //    return Ok(addresses);
            var model = _customerModelFactory.PrepareCustomerAddressListModel();
            return Ok(model);

        }


        //public virtual IActionResult AddressEdit(int addressId, string mobileno)
        //{
        //    var customer = _customerService.GetCustomerByUsername(mobileno);
        //    _workContext.CurrentCustomer = customer;

        //    if (!_customerService.IsRegistered(_workContext.CurrentCustomer))
        //        return Challenge();

        //    //var customer = _workContext.CurrentCustomer;
        //    //find address (ensure that it belongs to the current customer)
        //    var address = _customerService.GetCustomerAddress(customer.Id, addressId);
        //    if (address == null)
        //        //address is not found
        //        return Ok(address);

        //    var model = new CustomerAddressEditModel();
        //    _addressModelFactory.PrepareAddressModel(model.Address,
        //        address: address,
        //        excludeProperties: false,
        //        addressSettings: _addressSettings,
        //        loadCountries: () => _countryService.GetAllCountries(_workContext.WorkingLanguage.Id));

        //    return Ok(model);
        //}
        [HttpPost("api/address/edit")]
          public virtual IActionResult AddressEdit([FromBody]Address address, int addressId, string mobileno)
        {

            var customer = _customerService.GetCustomerByUsername(mobileno);
            _workContext.CurrentCustomer = customer;

            if (!_customerService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            //var customer = _workContext.CurrentCustomer;
            //find address (ensure that it belongs to the current customer)
            var addresscheck = _customerService.GetCustomerAddress(customer.Id, addressId);
            if (addresscheck == null)
                //address is not found
                return BadRequest();

            //custom address attributes
            //var customAttributes = _addressAttributeParser.ParseCustomAddressAttributes(form);
            //var customAttributeWarnings = _addressAttributeParser.GetAttributeWarnings(customAttributes);
            //foreach (var error in customAttributeWarnings)
            //{
            //    ModelState.AddModelError("", error);
            //}

            //if (ModelState.IsValid)
            //{
              
            if (address.CountryId == 0 || address.CountryId == null)
                address.CountryId = 131;
            // address.CustomAttributes = customAttributes;
            _addressService.UpdateAddress(address);

            //var addresses = _customerService.GetAddressesByCustomerId(customer.Id);
            //return Ok(addresses);
            var model = _customerModelFactory.PrepareCustomerAddressListModel();
            return Ok(model);
            ////}

            ////If we got this far, something failed, redisplay form
            //_addressModelFactory.PrepareAddressModel(model.Address,
            //    address: address,
            //    excludeProperties: true,
            //    addressSettings: _addressSettings,
            //    loadCountries: () => _countryService.GetAllCountries(_workContext.WorkingLanguage.Id),
            //    overrideAttributesXml: null);
            //return View(model);
        }
    }
}
