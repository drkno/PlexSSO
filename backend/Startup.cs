using System;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlexSSO.Service.PlexClient;

namespace PlexSSO
{
    public class Startup
    {
        private const string PoweredByHeaderName = "X-Powered-By";
        private const string PoweredByHeaderValue = "One small piece of fairy cake";
        public IConfiguration Configuration { get; }
        

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews().AddJsonOptions(opt =>
            {
                opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ui";
            });

            services.AddHttpClient();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.AccessDeniedPath = "/sso/403";
                    options.Cookie.Name = "kPlexSSOKookieV2";
                    options.LoginPath = "/api/v2/login";
                    options.LogoutPath = "/api/v2/logout";
                    options.ExpireTimeSpan = TimeSpan.FromDays(1);
                    var cookieDomain = Configuration["cookie_domain"];
                    if (!string.IsNullOrWhiteSpace(cookieDomain))
                    {
                        options.Cookie.Domain = cookieDomain;
                    }
                });
            services.AddHealthChecks();
            services.AddSingleton<IPlexClient, Client>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/sso/403");
            }
            app.Use((context, next) => {
                context.Response.Headers.Add(PoweredByHeaderName, PoweredByHeaderValue);
                return next.Invoke();
            });
            app.UseStaticFiles();
            app.UseSpaStaticFiles();
            app.UseRouting();
            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ui";
            });

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(name: "default", pattern: "{controller}/{action=Index}/{id?}");
                endpoints.MapHealthChecks("/api/v2/healthcheck");
            });
        }
    }
}
