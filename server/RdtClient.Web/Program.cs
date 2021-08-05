using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RdtClient.Data.Data;
using RdtClient.Data.Models.Internal;
using RdtClient.Service.Services;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;

namespace RdtClient.Web
{
    public class Program
    {
        public static readonly LoggingLevelSwitch LoggingLevelSwitch = new(LogEventLevel.Debug);

        public static async Task Main(String[] args)
        {
            try
            {
                var host = CreateHostBuilder(args).Build();

                // Perform migrations
                using (var scope = host.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                    await dbContext.Database.MigrateAsync();
                    await dbContext.Seed();

                    var logLevelSettingDb = await dbContext.Settings.FirstOrDefaultAsync(m => m.SettingId == "LogLevel");
                    
                    var logLevelSetting = "Warning";

                    if (logLevelSettingDb != null)
                    {
                        logLevelSetting = logLevelSettingDb.Value;
                    }

                    if (!Enum.TryParse<LogEventLevel>(logLevelSetting, out var logLevel))
                    {
                        logLevel = LogEventLevel.Warning;
                    }

                    LoggingLevelSwitch.MinimumLevel = logLevel;
                }

                var version = Assembly.GetEntryAssembly()?.GetName().Version;
                Log.Warning($"Starting host on version {version}");
                
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static IHostBuilder CreateHostBuilder(String[] args)
        {
            var configuration = new ConfigurationBuilder()
#if DEBUG
                                .AddJsonFile("appsettings.Development.json", true, false)
#else
                                .AddJsonFile("appsettings.json", true, false)
#endif
                                .Build();

            var appSettings = new AppSettings();
            configuration.Bind(appSettings);

            if (String.IsNullOrWhiteSpace(appSettings.HostUrl))
            {
                appSettings.HostUrl = "http://0.0.0.0:6500";
            }
            
            Log.Logger = new LoggerConfiguration()
                         .Enrich.FromLogContext()
                         .Enrich.WithExceptionDetails()
                         .WriteTo.File(appSettings.Logging.File.Path, rollOnFileSizeLimit: true, fileSizeLimitBytes: appSettings.Logging.File.FileSizeLimitBytes, retainedFileCountLimit: appSettings.Logging.File.MaxRollingFiles)
                         .WriteTo.Console()
                         .MinimumLevel.ControlledBy(LoggingLevelSwitch)
                         .CreateLogger();

            Serilog.Debugging.SelfLog.Enable(msg =>
            {
                Debug.Print(msg);
                Debugger.Break();
                Console.WriteLine(msg);
                Debug.WriteLine(msg);
            });

            return Host.CreateDefaultBuilder(args)
                       .UseWindowsService()
                       .ConfigureServices((hostContext, services) =>
                       {
                           services.AddHostedService<TaskRunner>();
                       })
                       .ConfigureWebHostDefaults(webBuilder =>
                       {
                           webBuilder.UseUrls(appSettings.HostUrl)
                                     .UseSerilog()
                                     .UseKestrel()
                                     .UseStartup<Startup>();
                       });
        }
    }
}
