using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text;
using Nop.Core;
using Nop.Core.Domain.CampaignPromo;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Services.Customers;

namespace Nop.Services.CampaignPromo
{
    public interface ICampaignPromoService
    {

         bool ValidPrelaunchPurchase(Product product);
    }

    public class CampaignPromoService : ICampaignPromoService
    {
        //   List<User> users;

        private readonly IRepository<CampaignMaster> _campaignMaster;
        private readonly IRepository<CampaignUserPurchase> _campaignUserPurchase;
        private readonly IRepository<CampaignProduct> _campaignProduct;
        private readonly IRepository<CampaignCustomerRole> _campaignCustomerRole;
        private readonly ICustomerService _customerService;
        private readonly IWorkContext _workContext;


        public CampaignPromoService(IRepository<CampaignMaster> campaignMaster, IRepository<CampaignUserPurchase> campaignUserPurchase, ICustomerService customerService, IWorkContext workContext, IRepository<CampaignProduct> campaignProduct, IRepository<CampaignCustomerRole> campaignCustomerRole)
        {
            _campaignMaster = campaignMaster;
            _campaignUserPurchase = campaignUserPurchase;
            _customerService = customerService;
            _workContext = workContext;
            _campaignProduct = campaignProduct;
            _campaignCustomerRole = campaignCustomerRole;


        }
        public bool ValidPrelaunchPurchase(Product product) {
            var customer = _workContext.CurrentCustomer;
            var customerRoles = _customerService.GetCustomerRoles(customer).Select(a => a.Id).ToList();
            

            var validcampaign = (from cm in _campaignMaster.Table
                         join cp in _campaignProduct.Table on cm.CampaignUid equals cp.CampaignUid
                         join ccr in _campaignCustomerRole.Table on cm.CampaignUid equals ccr.CampaignUid
                         where cm.EndEffectiveStamp < DateTime.Now 
                         && cp.ProductID == product.Id
                         && customerRoles.Contains(ccr.CustomerRoleId)
                         select cm).FirstOrDefault();
            if (validcampaign == null)
                return false;
            var campaignPurchase = _campaignUserPurchase.Table.Where(a => a.CampaignId == validcampaign.Id).FirstOrDefault();
            if (campaignPurchase == null)
                return true;
            if (campaignPurchase.CampaignPurchases >= campaignPurchase.CampaignPurchaseLimit)
                return false;
            else
             return true;
        
        }
    }
 
}
