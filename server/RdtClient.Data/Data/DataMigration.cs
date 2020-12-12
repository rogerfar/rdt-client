using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RdtClient.Data.Models.Data;

namespace RdtClient.Data.Data
{
    public static class DataMigration
    {
        private static Boolean InDocker => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

        public static void Setup(IServiceScope serviceScope)
        {
            using var scope = serviceScope.ServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();

            var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            dataContext.Database.Migrate();

            Seed(dataContext);
        }

        private static void Seed(DataContext dataContext)
        {
            var defaultDownloadPath = "/data/downloads";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !InDocker)
            {
                defaultDownloadPath = @"C:\Downloads";
            }
            
            var configuration = dataContext.Settings.FirstOrDefault();

            if (configuration == null)
            {
                dataContext.Settings.Add(new Setting
                {
                    SettingId = "RealDebridApiKey",
                    Type = "String",
                    Value = ""
                });

                dataContext.Settings.Add(new Setting
                {
                    SettingId = "DownloadFolder",
                    Type = "String",
                    Value = defaultDownloadPath
                });
                
                dataContext.Settings.Add(new Setting
                {
                    SettingId = "DownloadLimit",
                    Type = "Int32",
                    Value = "10"
                });

                dataContext.SaveChanges();
            }
        }
    }
}
