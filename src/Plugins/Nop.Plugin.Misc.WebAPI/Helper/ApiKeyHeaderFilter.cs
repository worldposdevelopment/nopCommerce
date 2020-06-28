using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Nop.Plugin.Misc.WebAPI.Helper
{
    public class ApiKeyHeaderFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                return;

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "ApiKey",
                In = ParameterLocation.Header,

                Required = true // set to false if this is optional
            });
        }



    }
}
