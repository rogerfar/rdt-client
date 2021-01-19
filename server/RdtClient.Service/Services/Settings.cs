using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
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
        Task<Double> TestDownloadSpeed();
        Task<Double> TestWriteSpeed(String path);
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

        public async Task<Double> TestDownloadSpeed()
        {
            var watch = new Stopwatch();

            var request = WebRequest.Create(new Uri("https://34.download.real-debrid.com/speedtest/testDefault.rar/" + DateTime.Now.Ticks));

            watch.Start();

            using var response = await request.GetResponseAsync();

            await using var stream = response.GetResponseStream();

            if (stream == null)
            {
                throw new IOException("No stream");
            }

            var buffer = new Byte[64 * 1024];
            Int64 totalRead = 0;

            while (totalRead < response.ContentLength)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));

                if (read > 0)
                {
                    totalRead += read;
                }
                else
                {
                    break;
                }
            }

            watch.Stop();

            var downloadSpeed = totalRead / watch.Elapsed.TotalSeconds;

            return downloadSpeed;
        }

        public async Task<Double> TestWriteSpeed(String path)
        {
            var testFilePath = Path.Combine(path, "test.tmp");

            const Int32 testFileSize = 1024 * 1024 * 1024;

            var watch = new Stopwatch();

            if (File.Exists(testFilePath))
            {
                File.Delete(testFilePath);
            }
            
            watch.Start();

            var rnd = new Random();
            
            await using var fileStream = new FileStream(testFilePath, FileMode.Create, FileAccess.Write, FileShare.Write);

            var buffer = new Byte[64 * 1024];
            
            while (fileStream.Length < testFileSize)
            {
                rnd.NextBytes(buffer);

                fileStream.Write(buffer, 0, buffer.Length);
            }

            watch.Stop();

            var writeSpeed = fileStream.Length / watch.Elapsed.TotalSeconds;

            return writeSpeed;
        }
    }
}
