using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using AutoMapper;
using Microsoft.OpenApi.Models;
using Nop.Core.Infrastructure;
using Nop.Web.Framework.Infrastructure;

namespace Nop.Plugin.Misc.WebAPI
{
    public class WebApiStartup : INopStartup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940

        //   public IConfiguration Configuration { get; }

        public int Order => new AuthenticationStartup().Order + 1;

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.


        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {

            // services.AddAutoMapper(typeof(Startup));
            //     services.AddDbContext<DataContext>(options => options.UseSqlServer(Configuration["ConnectionStrings:hoopsstationDB"])); ;

            services.AddSwaggerGen(c => {
               c.SwaggerDoc("v1", new OpenApiInfo { Title = "Hoopsstation API", Version = "v1" });

            });
          //  services.AddIdentity<User, Role>().AddEntityFrameworkStores<DataContext>().AddDefaultTokenProviders().AddRoleValidator<RoleValidator<Role>>().AddRoleManager<RoleManager<Role>>().AddSignInManager<SignInManager<User>>();

            var key = Encoding.ASCII.GetBytes("TestJWT");
            services.AddAuthentication(x => {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x => {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });
            services.AddHttpClient();
            // services.AddAutoMapper(typeof(Startup));
            // services.AddScoped<IUserService, UserValidationService>();
            //services.AddControllers(options =>
            //{

            //    var policy = new AuthorizationPolicyBuilder()
            //    .RequireAuthenticatedUser()
            //    .Build();
            //    options.Filters.Add(new AuthorizeFilter(policy));
            //});
            // services.AddControllers();
        }

        public void Configure(IApplicationBuilder application)
        {

        //    application.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
           // application.UseRouting();
            application.UseSwagger();

            application.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("../swagger/v1/swagger.json", "My API V1");
            }
            );
        

        }
    }
}
