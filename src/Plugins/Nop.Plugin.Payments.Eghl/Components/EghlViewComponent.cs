using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.Eghl.Components
{
    [ViewComponent(Name = "Eghl")]
    public class EghlViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Plugins/Payments.Eghl/Views/PaymentInfo.cshtml");
        }
    }
}
