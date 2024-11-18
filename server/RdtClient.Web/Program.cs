using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting.WindowsServices;
using RdtClient.Data.Data;
using RdtClient.Data.Models.Internal;
using RdtClient.Service;
using RdtClient.Service.Middleware;
using RdtClient.Service.Services;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default
});

// Bind the AppSettings from the appsettings.json files.
builder.Configuration.AddJsonFile("appsettings.json", false, false);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, false);

// Bind AppSettings
var appSettings = new AppSettings();
builder.Configuration.Bind(appSettings);
builder.Services.AddSingleton(appSettings);

// Configure URLs
if (appSettings.Port <= 0)
{
    appSettings.Port = 6500;
}

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(appSettings.Port);
});

if (appSettings.Logging?.File?.Path != null)
{
    builder.Host.UseSerilog((_, lc) => lc.Enrich.FromLogContext()
                                         .WriteTo.File(appSettings.Logging.File.Path,
                                                       rollOnFileSizeLimit: true,
                                                       fileSizeLimitBytes: appSettings.Logging.File.FileSizeLimitBytes,
                                                       retainedFileCountLimit: appSettings.Logging.File.MaxRollingFiles,
                                                       outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                                                       restrictedToMinimumLevel: LogEventLevel.Verbose)
                                         .WriteTo.Console()
                                         .MinimumLevel.ControlledBy(Settings.LoggingLevelSwitch)
                                         .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                                         .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning));
}

Serilog.Debugging.SelfLog.Enable(msg =>
{
    Debug.Print(msg);
    Debugger.Break();
    Console.WriteLine(msg);
    Debug.WriteLine(msg);
});

Log.Information("Starting RealDebridClient host");

builder.Services.AddControllers();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
       .AddCookie(options =>
       {
           options.SlidingExpiration = true;
       });


builder.Services.AddAuthorizationBuilder().AddPolicy("AuthSetting", policyCorrectUser =>
{
    policyCorrectUser.Requirements.Add(new AuthSettingRequirement());
});


builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
       {
           options.User.RequireUniqueEmail = false;
           options.Password.RequiredLength = 5;
           options.Password.RequireUppercase = false;
           options.Password.RequireLowercase = false;
           options.Password.RequireNonAlphanumeric = false;
           options.Password.RequiredUniqueChars = 3;
       })
       .AddEntityFrameworkStores<DataContext>()
       .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Cookie.Name = "SID";
});

builder.Services.Configure<HostOptions>(hostOptions =>
{
    hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

// Configure development cors.
builder.Services.AddCors(options =>
{
    options.AddPolicy("Dev",
                      corsBuilder => corsBuilder.AllowAnyMethod()
                                                .AllowAnyHeader()
                                                .AllowCredentials());
});

// Configure misc services.
builder.Services.AddResponseCaching();
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.RegisterHttpClients();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession();

builder.Services.AddSpaStaticFiles(spaBuilder =>
{
    spaBuilder.RootPath = "wwwroot";
});

builder.Services.AddSignalR(hubOptions =>
{
    hubOptions.EnableDetailedErrors = true;
});

builder.Host.UseWindowsService();

RdtClient.Data.DiConfig.Config(builder.Services, appSettings);
builder.Services.RegisterRdtServices();

try
{
    // Build the app
    var app = builder.Build();

    if (builder.Environment.IsDevelopment())
    {
        app.UseCors("Dev");
        app.UseDeveloperExceptionPage();
    }

    app.ConfigureExceptionHandler();

    app.UseMiddleware<AuthorizeMiddleware>();

    app.Use(async (context, next) =>
    {
        await next.Invoke();

        if (context.Response.StatusCode != 200)
        {
            Log.Warning("{StatusCode}: {Value}", context.Response.StatusCode, context.Request.Path.Value);
        }
    });

    var basePath = !String.IsNullOrWhiteSpace(appSettings.BasePath) ? appSettings.BasePath : !String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BASE_PATH")) ? Environment.GetEnvironmentVariable("BASE_PATH") : null;

    if (basePath != null)
    {
        app.UseMiddleware<BaseHrefMiddleware>(basePath);
        app.UsePathBase($"/{basePath.TrimStart('/').TrimEnd('/')}/");
    }

    app.UseMiddleware<RequestLoggingMiddleware>();

    app.UseRouting();

    app.UseAuthentication();

    app.UseAuthorization();

    app.MapHub<RdtHub>("/hub");

    app.MapControllers();

    app.UseWhen(x => !x.Request.Path.StartsWithSegments("/api"), routeBuilder =>
    {
        routeBuilder.UseSpaStaticFiles();
        routeBuilder.UseSpa(spa =>
        {
            spa.Options.SourcePath = "wwwroot";
            spa.Options.DefaultPage = "/index.html";
        });
    });
    
    // Run the app
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}