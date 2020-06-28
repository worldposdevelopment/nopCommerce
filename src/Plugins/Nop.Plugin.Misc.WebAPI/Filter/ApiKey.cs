using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Nop.Plugin.Misc.WebAPI.Filter
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]

    public class ApiKeyAuthAttribute : Attribute, IAsyncActionFilter
    {
        private const string ApiKeyHeaderName = "ApiKey";





        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            //LicenseContext _context = context.HttpContext.RequestServices.GetService<LicenseContext>();
            if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var potentialApiKey))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            //var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            //var apiKey = configuration.GetValue<string>("ApiKey");
            if (!("1qazXSW@" == potentialApiKey.ToString()))
            {
                context.Result = new UnauthorizedResult();
                return;
            }


            //    if (!apiKey.Equals(potentialApiKey))
            //{
            //    context.Result = new UnauthorizedResult();
            //    return;
            //}

            await next();
        }

    }
}
