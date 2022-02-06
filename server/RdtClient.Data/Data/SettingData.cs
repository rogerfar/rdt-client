using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.Internal;
using Serilog;

namespace RdtClient.Data.Data
{
    public class SettingData
    {
        private static readonly SemaphoreSlim _settingCacheLock = new(1);

        private readonly DataContext _dataContext;

        public SettingData(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public static DbSettings Get { get; private set; }

        public async Task ResetCache()
        {
            var allSettings = await _dataContext.Settings.AsNoTracking().ToListAsync();

            String GetString(String name)
            {
                return allSettings.FirstOrDefault(m => m.SettingId == name)?.Value;
            }

            Int32 GetInt32(String name)
            {
                var strVal = GetString(name);

                if (!Int32.TryParse(strVal, out var intVal))
                {
                    Log.Error("Unable to parse setting {name} to Int32", name);
                    return 0;
                }

                return intVal;

            }

            Get = new DbSettings
            {
                Provider = GetString("Provider"),
                ProviderAutoImport = GetInt32("ProviderAutoImport"),
                ProviderAutoImportCategory = GetString("ProviderAutoImportCategory"),
                ProviderAutoDelete = GetInt32("ProviderAutoDelete"),
                RealDebridApiKey = GetString("RealDebridApiKey"),
                DownloadPath = GetString("DownloadPath"),
                DownloadClient = GetString("DownloadClient"),
                TempPath = GetString("TempPath"),
                MappedPath = GetString("MappedPath"),
                DownloadLimit = GetInt32("DownloadLimit"),
                UnpackLimit = GetInt32("UnpackLimit"),
                MinFileSize = GetInt32("MinFileSize"),
                OnlyDownloadAvailableFiles = GetInt32("OnlyDownloadAvailableFiles"),
                DownloadChunkCount = GetInt32("DownloadChunkCount"),
                DownloadMaxSpeed = GetInt32("DownloadMaxSpeed"),
                ProxyServer = GetString("ProxyServer"),
                LogLevel = GetString("LogLevel"),
                Categories = GetString("Categories"),
                Aria2cUrl = GetString("Aria2cUrl"),
                Aria2cSecret = GetString("Aria2cSecret"),
                DownloadRetryAttempts = GetInt32("DownloadRetryAttempts"),
                TorrentRetryAttempts = GetInt32("TorrentRetryAttempts"),
                DeleteOnError = GetInt32("DeleteOnError"),
                TorrentLifetime = GetInt32("TorrentLifetime"),
            };
        }

        public async Task<IList<Setting>> GetAll()
        {
            return await _dataContext.Settings.AsNoTracking().ToListAsync();
        }

        public async Task Update(IList<Setting> settings)
        {
            await _settingCacheLock.WaitAsync();

            try
            {
                var dbSettings = await _dataContext.Settings.ToListAsync();

                foreach (var dbSetting in dbSettings)
                {
                    var setting = settings.FirstOrDefault(m => m.SettingId == dbSetting.SettingId);

                    if (setting != null)
                    {
                        dbSetting.Value = setting.Value;
                    }
                }

                await _dataContext.SaveChangesAsync();

                await ResetCache();
            }
            finally
            {
                _settingCacheLock.Release();
            }
        }

        public async Task UpdateString(String key, String value)
        {
            await _settingCacheLock.WaitAsync();

            try
            {
                var dbSetting = await _dataContext.Settings.FirstOrDefaultAsync(m => m.SettingId == key);

                if (dbSetting == null)
                {
                    throw new Exception($"Cannot find setting with key {key}");
                }
                
                dbSetting.Value = value;

                await _dataContext.SaveChangesAsync();

                await ResetCache();
            }
            finally
            {
                _settingCacheLock.Release();
            }
        }
    }
}
