using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Noodle.WebCore.Api.Data;

namespace Noodle.WebCore.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<PortalUserDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("PortalUserConnection")));

            services.AddIdentity<PortalUser, PortalRole>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<PortalUserDbContext>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "JwtBearer";
                options.DefaultChallengeScheme = "JwtBearer";
            })
                .AddJwtBearer("JwtBearer", options => 
                {  
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        IssuerSigningKey = GetSigningKey(),
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidIssuer = GetTokenIssuer(),
                        ValidAudience = GetTokenAudience(),
                        ValidateLifetime = true
                    };
                });

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Noodle.WebCore.Api", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Noodle.WebCore.Api v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        /// <summary>
        /// Get signing key from configuration
        /// </summary>
        /// <returns></returns>
        private SecurityKey GetSigningKey()
        {
            return new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    "ShVmYq3s6v9y$B&E)H@McQfTjWnZr4u7"));
                    //Configuration["Jwt:Symmetric:Key"]));
        }

        /// <summary>
        /// Get a valid TokenAudience
        /// </summary>
        /// <returns></returns>
        private string GetTokenAudience()
        {
            //TODO Get from configuration
            //TODO Move to a configuration factory and share reference in the AccountController
            return "http://localhost";
        }

        /// <summary>
        /// Get a valid TokenIssuer
        /// </summary>
        /// <returns></returns>
        private string GetTokenIssuer()
        {
            //TODO Get from configuration
            //TODO Move to a configuration factory and share reference in the AccountController
            return "http://localhost";
        }
    }
}
