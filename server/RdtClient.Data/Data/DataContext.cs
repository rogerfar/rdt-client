using System;
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

        public DataContext()
        {
        }

        public static String ConnectionString => $"Data Source={AppContext.BaseDirectory}database.db";

        public DbSet<Download> Downloads { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<Torrent> Torrents { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite(ConnectionString);

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Setting>()
                   .HasData(new Setting
                   {
                       SettingId = "RealDebridApiKey",
                       Type = "String",
                       Value = ""
                   });

            builder.Entity<Setting>()
                   .HasData(new Setting
                   {
                       SettingId = "DownloadFolder",
                       Type = "String",
                       Value = @"C:\Downloads"
                   });

            builder.Entity<Setting>()
                   .HasData(new Setting
                   {
                       SettingId = "DownloadLimit",
                       Type = "Int32",
                       Value = "10"
                   });
        }

        public void Migrate()
        {
            Database.Migrate();
        }
    }
}