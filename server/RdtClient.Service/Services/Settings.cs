using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using RdtClient.Data.Data;
using RdtClient.Data.Models.Data;

namespace RdtClient.Service.Services
{
    public interface ISettings
    {
        Task<IList<Setting>> GetAll();
        Task Update(IList<Setting> settings);
        Task<String> GetString(String key);
        Task<Int32> GetNumber(String key);
        Task TestPath(String path);
    }

    public class Settings : ISettings
    {
        private readonly ISettingData _settingData;

        public Settings(ISettingData settingData)
        {
            _settingData = settingData;
        }

        public async Task<IList<Setting>> GetAll()
        {
            return await _settingData.GetAll();
        }

        public async Task Update(IList<Setting> settings)
        {
            await _settingData.Update(settings);
        }

        public async Task<String> GetString(String key)
        {
            var setting = await _settingData.Get(key);

            if (setting == null)
            {
                throw new Exception($"Setting with key {key} not found");
            }
            
            return setting.Value;
        }

        public async Task<Int32> GetNumber(String key)
        {
            var setting = await _settingData.Get(key);

            if (setting == null)
            {
                throw new Exception($"Setting with key {key} not found");
            }
            
            return Int32.Parse(setting.Value);
        }

        public async Task TestPath(String path)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                throw new Exception("Path is not set");
            }

            path = path.TrimEnd('/').TrimEnd('\\');

            if (!Directory.Exists(path))
            {
                throw new Exception($"Path {path} does not exist");
            }

            var testFile = $"{path}/test.txt";

            await File.WriteAllTextAsync(testFile, "RealDebridClient Test File, you can remove this file.");
            File.Delete(testFile);
        }
    }
}
