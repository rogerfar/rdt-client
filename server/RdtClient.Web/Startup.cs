using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RdtClient.Data.Data;
using RdtClient.Data.Models.Internal;
using RdtClient.Service.Middleware;
using RdtClient.Service.Services;

namespace RdtClient.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var appSettings = new AppSettings();
            Configuration.Bind(appSettings);

            services.AddSingleton(appSettings);

            var connectionString = $"Data Source={appSettings.Database.Path}";
            services.AddDbContext<DataContext>(options => options.UseSqlite(connectionString));

            services.AddControllers();

            services.AddSpaStaticFiles(configuration => { configuration.RootPath = "wwwroot"; });

            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });
            
            services.AddHttpContextAccessor();

            services.AddCors(options =>
            {
                options.AddPolicy("Dev",
                                  builder =>
                                  {
                                      builder.AllowAnyHeader()
                                             .AllowAnyMethod()
                                             .AllowAnyOrigin();
                                  });
            });

            services.AddHttpClient();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(options => { options.SlidingExpiration = true; });

            services.AddAuthorization();

            services.AddIdentity<IdentityUser, IdentityRole>(options =>
                    {
                        options.User.RequireUniqueEmail = false;
                        options.Password.RequiredLength = 10;
                        options.Password.RequireUppercase = false;
                        options.Password.RequireLowercase = false;
                        options.Password.RequireNonAlphanumeric = false;
                        options.Password.RequiredUniqueChars = 5;
                    })
                    .AddEntityFrameworkStores<DataContext>()
                    .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(options =>
            {
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Cookie.Name = "SID";
            });

            Data.DiConfig.Config(services);
            Service.DiConfig.Config(services);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseCors("Dev");
            }

            app.ConfigureExceptionHandler();

            app.UseMiddleware<AuthorizeMiddleware>();

            app.Use(async (context, next) =>
            {
                await next.Invoke();

                if (context.Response.StatusCode != 200)
                {
                    logger.LogWarning("{StatusCode}: {Value}", context.Response.StatusCode, context.Request.Path.Value);
                }
            });

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<RdtHub>("/hub");
                endpoints.MapControllers();
            });

            app.MapWhen(x => !x.Request.Path.Value.StartsWith("/api"), builder =>
            {
                builder.UseSpaStaticFiles();
                builder.UseSpa(spa =>
                {
                    spa.Options.SourcePath = "wwwroot";
                    spa.Options.DefaultPage = "/index.html";
                });
            });
        }
    }
}