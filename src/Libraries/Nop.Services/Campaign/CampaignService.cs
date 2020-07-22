using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text;
using LinqToDB;
using LinqToDB.Tools;
using Nop.Core;
using Nop.Core.Domain.CampaignPromo;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Services.Customers;

namespace Nop.Services.CampaignPromo
{
    public interface ICampaignPromoService
    {

         bool ValidPrelaunchPurchase(Product product, int qty);
        bool ValidRafflePurchase(Product product, int qty);
        void UpdateCampaignPurchase(Product product, int qty);
        int GetStorePickupPointOfflineRaffle(Product product);
    }

    public class CampaignPromoService : ICampaignPromoService
    {
        //   List<User> users;

        private readonly IRepository<CampaignMaster> _campaignMaster;
        private readonly IRepository<CampaignUserPurchase> _campaignUserPurchase;
        private readonly IRepository<CampaignProduct> _campaignProduct;
        private readonly IRepository<CampaignCustomerRole> _campaignCustomerRole;
        private readonly IRepository<CampaignRaffle> _campaignRaffle;
        private readonly ICustomerService _customerService;
        private readonly IWorkContext _workContext;


        public CampaignPromoService(IRepository<CampaignMaster> campaignMaster, IRepository<CampaignUserPurchase> campaignUserPurchase, ICustomerService customerService, IWorkContext workContext, IRepository<CampaignProduct> campaignProduct, 
            IRepository<CampaignCustomerRole> campaignCustomerRole, IRepository<CampaignRaffle> campaignRaffle)
        {
            _campaignMaster = campaignMaster;
            _campaignUserPurchase = campaignUserPurchase;
            _customerService = customerService;
            _workContext = workContext;
            _campaignProduct = campaignProduct;
            _campaignCustomerRole = campaignCustomerRole;
            _campaignRaffle = campaignRaffle;


        }
        public bool ValidPrelaunchPurchase(Product product, int qty) {
            var customer = _workContext.CurrentCustomer;
            var customerRoles = _customerService.GetCustomerRoles(customer).Select(a => a.Id).ToList();
            

            var validcampaign = (from cm in _campaignMaster.Table
                         join cp in _campaignProduct.Table on cm.CampaignUid equals cp.CampaignUid
                         join ccr in _campaignCustomerRole.Table on cm.CampaignUid equals ccr.CampaignUid
                         where cm.EndEffectiveStamp > DateTime.Now 
                         && cp.ProductID == product.Id
                         && customerRoles.Contains(ccr.CustomerRoleId)
                         orderby cm.Id descending
                         select new CampaignMaster { Id = cm.Id, MaxPurchase = cp.MaxPurchase}).FirstOrDefault();

            if (validcampaign == null)
                return false;
            var campaignPurchase = _campaignUserPurchase.Table.Where(a => a.CampaignId == validcampaign.Id && a.UserId == customer.Id).FirstOrDefault();
            if (campaignPurchase == null && qty <= validcampaign.MaxPurchase)
                return true;
            if (qty > validcampaign.MaxPurchase)
                return false;
            if (campaignPurchase.CampaignPurchases+qty > campaignPurchase.CampaignPurchaseLimit)
                return false;
            else
             return true;
        
        }
        public bool ValidRafflePurchase(Product product, int qty)
        {
            var customer = _workContext.CurrentCustomer;
            var customerRoles = _customerService.GetCustomerRoles(customer).Select(a => a.Id).ToList();


            var validcampaignId = (from cr in _campaignRaffle.Table
                                 where cr.IsWinner == true && cr.CustomerMobile == customer.Username && cr.ProductId == product.Id
                                   orderby cr.Id descending
                                   select cr.CampaignId).FirstOrDefault();
            if (validcampaignId == 0)
                return false;
            var validcampaign = (from cm in _campaignMaster.Table
                                  join cp in _campaignProduct.Table on cm.CampaignUid equals cp.CampaignUid
                                  where cm.Id == validcampaignId && cp.ProductID == product.Id
                                 orderby cm.Id descending
                                 select new CampaignMaster { Id = cm.Id, MaxPurchase = cp.MaxPurchase }).FirstOrDefault();
         


            var campaignPurchase = _campaignUserPurchase.Table.Where(a => a.CampaignId == validcampaign.Id && a.UserId == customer.Id).FirstOrDefault();
            if (campaignPurchase == null && qty <= validcampaign.MaxPurchase)
                return true;
            if (qty > validcampaign.MaxPurchase)
                return false;
            if (campaignPurchase.CampaignPurchases + qty > campaignPurchase.CampaignPurchaseLimit)
                return false;
            else
                return true;

        }
        public void UpdateCampaignPurchase(Product product, int qty)
        {
            var customer = _workContext.CurrentCustomer;
            var customerRoles = _customerService.GetCustomerRoles(customer).Select(a => a.Id).ToList();
            var validcampaign = new CampaignMaster();
            if (product.IsPrelaunch == true)
                validcampaign = (from cm in _campaignMaster.Table
                                 join cp in _campaignProduct.Table on cm.CampaignUid equals cp.CampaignUid
                                 join ccr in _campaignCustomerRole.Table on cm.CampaignUid equals ccr.CampaignUid
                                 where cm.EndEffectiveStamp > DateTime.Now
                                 && cp.ProductID == product.Id
                                 && customerRoles.Contains(ccr.CustomerRoleId)
                                 orderby cm.Id descending
                                 select new CampaignMaster { Id = cm.Id, MaxPurchase = cp.MaxPurchase }).FirstOrDefault();
            else
            if (product.IsOfflineRafflePrize == true || product.IsOnlineRafflePrize == true)
                validcampaign = (from cr in _campaignRaffle.Table
                                 join cm in _campaignMaster.Table on cr.CampaignId equals cm.Id
                                 join cp in _campaignProduct.Table on cm.CampaignUid equals cp.CampaignUid
                                 where cr.IsWinner == true && cr.CustomerMobile == customer.Username && cp.ProductID == product.Id
                                 orderby cm.Id descending
                                 select new CampaignMaster { Id = cm.Id, MaxPurchase = cp.MaxPurchase }).FirstOrDefault();
            else
                return;

            if (validcampaign == null)
                return;
            var campaignPurchase = _campaignUserPurchase.Table.Where(a => a.CampaignId == validcampaign.Id && a.UserId == customer.Id).FirstOrDefault();
            if (campaignPurchase == null)
                _campaignUserPurchase.Insert(new CampaignUserPurchase { CampaignId = validcampaign.Id, CampaignPurchaseLimit = validcampaign.MaxPurchase ?? 0, CampaignPurchases = qty, UserId = customer.Id });
            else
            {
                campaignPurchase.CampaignPurchases = campaignPurchase.CampaignPurchases+qty;
                if (campaignPurchase.CampaignPurchases == 0)
                    campaignPurchase.CampaignPurchases = 0;
                _campaignUserPurchase.Update(campaignPurchase);

                    }
            //if(product.IsOfflineRafflePrize || product.IsOnlineRafflePrize)
            //{ 
            //var campaignRaffle = _campaignRaffle.Table.Where(a => a.CampaignId == validcampaign.Id && a.CustomerMobile == customer.Username).FirstOrDefault();
            //if (campaignRaffle != null)
            //    campaignRaffle.HasClaimed = campaignRaffle.HasClaimed + qty;
            //_campaignRaffle.Update(campaignRaffle);
            //}

        }
        public int GetStorePickupPointOfflineRaffle(Product product) {
            var customer = _workContext.CurrentCustomer;
            var customerRoles = _customerService.GetCustomerRoles(customer).Select(a => a.Id).ToList();
            var campaignRaffle = (from cm in _campaignMaster.Table
                                  join cp in _campaignProduct.Table on cm.CampaignUid equals cp.CampaignUid
                                  join ccr in _campaignCustomerRole.Table on cm.CampaignUid equals ccr.CampaignUid
                                  join cr in _campaignRaffle.Table on cm.Id equals cr.CampaignId
                                  where cr.ProductId ==product.Id &&cr.CustomerMobile == customer.Username
                                 && cp.ProductID == product.Id
                                 && customerRoles.Contains(ccr.CustomerRoleId)
                                  orderby cm.Id descending
                                  select cr).FirstOrDefault();
            if (campaignRaffle == null)
                return 0;
            else
                return campaignRaffle.StoreId;


        }
    }
 
}
