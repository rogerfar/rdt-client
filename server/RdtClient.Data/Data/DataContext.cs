using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RdtClient.Data.Models.Data;

namespace RdtClient.Data.Data
{
    public class DataContext : IdentityDbContext
    {
        public DataContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Download> Downloads { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<Torrent> Torrents { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            var cascadeFKs = builder.Model.GetEntityTypes()
                                    .SelectMany(t => t.GetForeignKeys())
                                    .Where(fk => !fk.IsOwnership && fk.DeleteBehavior == DeleteBehavior.Cascade);

            foreach (var fk in cascadeFKs)
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }

        public async Task Seed()
        {
            var seedSettings = new List<Setting>
            {
                new Setting
                {
                    SettingId = "RealDebridApiKey",
                    Type = "String",
                    Value = ""
                },
                new Setting
                {
                    SettingId = "DownloadPath",
                    Type = "String",
                    #if DEBUG
                    Value = @"C:\Temp\rdtclient"
                    #else 
                    Value = "/data/downloads"
                    #endif
                },
                new Setting
                {
                    SettingId = "TempPath",
                    Type = "String",
#if DEBUG
                    Value = @"C:\Temp\rdtclient"
#else 
                    Value = "/data/downloads"
#endif
                },
                new Setting
                {
                    SettingId = "MappedPath",
                    Type = "String",
#if DEBUG
                    Value = @"C:\Temp\rdtclient"
#else 
                    Value = @"C:\Downloads"
#endif
                },
                new Setting
                {
                    SettingId = "DownloadLimit",
                    Type = "Int32",
                    Value = "2"
                },
                new Setting
                {
                    SettingId = "UnpackLimit",
                    Type = "Int32",
                    Value = "1"
                },
                new Setting
                {
                    SettingId = "MinFileSize",
                    Type = "Int32",
                    Value = "0"
                },
                new Setting
                {
                    SettingId = "DownloadChunkCount",
                    Type = "Int32",
                    Value = "8"
                },
                new Setting
                {
                    SettingId = "DownloadMaxSpeed",
                    Type = "Int32",
                    Value = "0"
                }
            };

            var dbSettings = await Settings.ToListAsync();
            foreach (var seedSetting in seedSettings)
            {
                var dbSetting = dbSettings.FirstOrDefault(m => m.SettingId == seedSetting.SettingId);

                if (dbSetting == null)
                {
                    await Settings.AddAsync(seedSetting);
                    await SaveChangesAsync();
                }
            }
        }
    }
}