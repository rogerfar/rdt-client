using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RdtClient.Data.Models.Data;

namespace RdtClient.Data.Data
{
    public class SettingData
    {
        private static IList<Setting> _settingCache;
        private static readonly SemaphoreSlim _settingCacheLock = new(1);

        private readonly DataContext _dataContext;

        public SettingData(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<IList<Setting>> GetAll()
        {
            await _settingCacheLock.WaitAsync();

            try
            {
                _settingCache ??= await _dataContext.Settings.AsNoTracking().ToListAsync();

                return _settingCache;
            }
            finally
            {
                _settingCacheLock.Release();
            }
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

                _settingCache = null;
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

                _settingCache = null;
            }
            finally
            {
                _settingCacheLock.Release();
            }
        }
    }
}
