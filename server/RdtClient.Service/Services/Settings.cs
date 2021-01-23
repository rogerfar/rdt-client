using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
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
        Task<Double> TestDownloadSpeed(CancellationToken cancellationToken);
        Task<Double> TestWriteSpeed();
        void Clean();
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

        public async Task<Double> TestDownloadSpeed(CancellationToken cancellationToken)
        {
            var downloadPath = await GetString("DownloadPath");

            var settings = await GetAll();

            var testFilePath = Path.Combine(downloadPath, "testDefault.rar");

            if (File.Exists(testFilePath))
            {
                File.Delete(testFilePath);
            }

            var download = new Download
            {
                Link = "https://34.download.real-debrid.com/speedtest/testDefault.rar",
                Torrent = new Torrent
                {
                    RdName = ""
                }
            };

            var downloadClient = new DownloadClient(download, downloadPath);

            await downloadClient.Start(settings);

            while (!downloadClient.Finished)
            {
                // ReSharper disable once MethodSupportsCancellation
                await Task.Delay(1000);
                
                if (cancellationToken.IsCancellationRequested)
                {
                    downloadClient.Cancel();
                }

                if (downloadClient.BytesDone > 1024 * 1024 * 50)
                {
                    downloadClient.Cancel();
                }
            }

            if (File.Exists(testFilePath))
            {
                File.Delete(testFilePath);
            }

            Clean();

            return downloadClient.Speed;
        }

        public async Task<Double> TestWriteSpeed()
        {
            var downloadPath = await GetString("DownloadPath");

            var testFilePath = Path.Combine(downloadPath, "test.tmp");

            if (File.Exists(testFilePath))
            {
                File.Delete(testFilePath);
            }

            const Int32 testFileSize = 64 * 1024 * 1024;

            var watch = new Stopwatch();

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
            
            fileStream.Close();

            if (File.Exists(testFilePath))
            {
                File.Delete(testFilePath);
            }

            return writeSpeed;
        }

        public void Clean()
        {
            try
            {
                var tempPath = GetString("TempPath").Result;

                if (!String.IsNullOrWhiteSpace(tempPath))
                {
                    var files = Directory.GetFiles(tempPath, "*.dsc", SearchOption.TopDirectoryOnly);

                    foreach (var file in files)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}
