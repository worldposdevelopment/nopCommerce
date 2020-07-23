using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB.Tools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Customers;
using Nop.Plugin.Misc.WebAPI.DTO;
using Nop.Plugin.Misc.WebAPI.Filter;
using Nop.Plugin.Misc.WebAPI.Services;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Logging;
using StackExchange.Profiling.Internal;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Nop.Plugin.Misc.WebAPI.Controllers
{
    [ApiKeyAuth]
    [Route("")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ICustomerRegistrationService _customerRegistrationService;
        private readonly CustomerSettings _customerSettings;
        private readonly ICustomerService _customerService;
        private readonly IUserService _userValidationService;
        private readonly ILogger _logger;
        private readonly IAddressService _addressService;
        private readonly IGenericAttributeService _genericAttributeService;
        public AuthController(ICustomerRegistrationService customerRegistrationService, CustomerSettings customerSettings, ICustomerService customerService, IUserService userValidationService, ILogger logger, IAddressService addressService, IGenericAttributeService genericAttributeService)
        {
            _customerRegistrationService = customerRegistrationService;
            _customerSettings = customerSettings;
            _customerService = customerService;
            _userValidationService = userValidationService;
            _logger = logger;
            _addressService = addressService;
            _genericAttributeService = genericAttributeService;
        }
        [AllowAnonymous]
        [HttpPost("api/auth/register")]
        public ActionResult RegisterUser([FromBody] UserForRegisterDTO model)
        {
            try
            { 
            if (String.IsNullOrEmpty(model.Email))
                _logger.Information("Email is empty", null, null);
            if (String.IsNullOrEmpty(model.Username))
                _logger.Information("Username is empty", null, null);
            if (String.IsNullOrEmpty(model.Email))
                 model.Email = model.Username + "@shop2.worldpos.com.my";

                _logger.Information(model.Email + model.Username+ model.Addresses.First(), null, null);
                var customer = _customerService.InsertGuestCustomer();
                var registrationRequest = new CustomerRegistrationRequest(customer,
                            model.Email,
                             model.Username,
                           "Worldpos2020!1qazXSW@",
                            _customerSettings.DefaultPasswordFormat,
                            1,
                            true);
                var result = _customerRegistrationService.RegisterCustomer(registrationRequest);
                if (result.Success)
                {
                    var address = model.Addresses.First();
                    
                    if (address.CountryId == 0 || address.CountryId == null)
                        address.CountryId = 131;
                    if (address.StateProvinceId == 0)
                        address.StateProvinceId = null;
            
                    _addressService.InsertAddress(model.Addresses.First());
                    _customerService.InsertCustomerAddress(customer, address);
                    customer.ShippingAddressId = _customerService.GetAddressesByCustomerId(customer.Id).Select (a => a.Id).First();
                    
                    _logger.Information("Insert address: Customer: "+ customer.Id +": address: "+ _customerService.GetAddressesByCustomerId(customer.Id).Select(a => a.Id).First(), null, null);

                    customer.BillingAddressId = _customerService.GetAddressesByCustomerId(customer.Id).Select(a => a.Id).First();
           
                    _customerService.UpdateCustomer(customer);
                    if (!String.IsNullOrEmpty(model.Gender))
                        _genericAttributeService.SaveAttribute(customer, NopCustomerDefaults.GenderAttribute, model.Gender[0]);
                    if (!String.IsNullOrEmpty(model.FirstName))
                        _genericAttributeService.SaveAttribute(customer, NopCustomerDefaults.FirstNameAttribute, model.FirstName);
                    if (!String.IsNullOrEmpty(model.LastName))
                        _genericAttributeService.SaveAttribute(customer, NopCustomerDefaults.LastNameAttribute, model.LastName);
                    if (model.DateOfBirth!= null)
                    {
                        var dateOfBirth = model.DateOfBirth;
                        _genericAttributeService.SaveAttribute(customer, NopCustomerDefaults.DateOfBirthAttribute, dateOfBirth);
                    }
                
                    _logger.Information(model.ToString(), null, null);
                }
                else
                { 
                    _logger.Information("Register failed: "+ result.ToJson(), null, null);
                }
        
               // _logger.Information(model.ToString(), null, null);
              
            
            return Ok();
            }catch(Exception ex){

                _logger.Information("Register failed ", ex, null);
                return BadRequest();

            }
        }
     
        //[HttpGet("api/auth/sendactivation")]
        //public ActionResult SendSmsActivation(String phoneNumber)
        //{
        //    var result = _userValidationService.SendSmsValidation(phoneNumber);
        //    return Ok(result);

        //}
        //[HttpGet("api/auth/confirmactivation")]
        //public ActionResult ConfirmSmsActivation(String phoneNumber, String phoneCode)
        //{
        //    var result = _userValidationService.ConfirmVerificationCode(phoneNumber, phoneCode);
        //    return Ok(result);

        //}
    
    [HttpGet("api/auth/updateroles")]
    public ActionResult RoleUpdate(String mobileno, String role)
    {
            var customer = _customerService.GetCustomerByUsername(mobileno);
            var roles = _customerService.GetCustomerRoles(customer);

            foreach (var customerrole in roles)
            {
                if (customerrole.Name != "Registered" &&
                    customerrole.Name != "DEV" &&
                    customerrole.Name != "Vendors")
                    _customerService.RemoveCustomerRoleMapping(customer, customerrole);




            }
            if (role == "Registered")
                return Ok();

              var newrole =_customerService.GetCustomerRoleBySystemName(role);
                var newcustomerrolemapping = new CustomerCustomerRoleMapping {
               CustomerId= customer.Id ,
               CustomerRoleId = newrole.Id
                };

             _customerService.AddCustomerRoleMapping(newcustomerrolemapping);
            return Ok();
       
          
    }

    //[HttpGet("api/auth/sendactivation")]
    //public ActionResult SendSmsActivation(String phoneNumber)
    //{
    //    var result = _userValidationService.SendSmsValidation(phoneNumber);
    //    return Ok(result);

    //}
    //[HttpGet("api/auth/confirmactivation")]
    //public ActionResult ConfirmSmsActivation(String phoneNumber, String phoneCode)
    //{
    //    var result = _userValidationService.ConfirmVerificationCode(phoneNumber, phoneCode);
    //    return Ok(result);

    //}
}
}

