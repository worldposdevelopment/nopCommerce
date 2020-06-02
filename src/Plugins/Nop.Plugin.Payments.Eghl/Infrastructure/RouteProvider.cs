using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.Eghl.Infrastructure
{
    public partial class RouteProvider : IRouteProvider
    {
        /// <summary>
        /// Register routes
        /// </summary>
        /// <param name="endpointRouteBuilder">Route builder</param>
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            //PDT
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.Eghl.PDTHandler", "Plugins/Eghl/PDTHandler",
                 new { controller = "Eghl", action = "PDTHandler" });

            //IPN
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.Eghl.IPNHandler", "Plugins/Eghl/IPNHandler",
                 new { controller = "Eghl", action = "IPNHandler" });

            //Cancel
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.Eghl.CancelOrder", "Plugins/Eghl/CancelOrder",
                 new { controller = "Eghl", action = "CancelOrder" });
        }

        /// <summary>
        /// Gets a priority of route provider
        /// </summary>
        public int Priority => -1;
    }
}