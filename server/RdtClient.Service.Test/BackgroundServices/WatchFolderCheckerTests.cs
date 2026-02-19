using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RdtClient.Data.Data;
using RdtClient.Data.Models.Data;
using RdtClient.Service.BackgroundServices;
using RdtClient.Service.Services;
using System.Reflection;

namespace RdtClient.Service.Test.BackgroundServices;

public class WatchFolderCheckerTests : IDisposable
{
    private readonly Mock<ILogger<WatchFolderChecker>> _loggerMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IServiceScope> _serviceScopeMock;
    private readonly Mock<IServiceProvider> _scopeServiceProviderMock;
    private readonly Mock<Torrents> _torrentsServiceMock;
    private readonly String _testPath;

    public WatchFolderCheckerTests()
    {
        _loggerMock = new Mock<ILogger<WatchFolderChecker>>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _scopeServiceProviderMock = new Mock<IServiceProvider>();
        _torrentsServiceMock = new Mock<Torrents>(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(new MockScopeFactory(_serviceScopeMock.Object));

        _serviceScopeMock
            .Setup(x => x.ServiceProvider)
            .Returns(_scopeServiceProviderMock.Object);

        _scopeServiceProviderMock
            .Setup(x => x.GetService(typeof(Torrents)))
            .Returns(_torrentsServiceMock.Object);

        _testPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testPath);

        // Reset Settings and Startup
        SetStartupReady(true);
        ResetSettings();
        Settings.Get.Watch.Path = _testPath;
    }

    public void Dispose()
    {
        if (Directory.Exists(_testPath))
        {
            Directory.Delete(_testPath, true);
        }
    }

    private static void SetStartupReady(Boolean ready)
    {
        var property = typeof(Startup).GetProperty("Ready", BindingFlags.Public | BindingFlags.Static);
        property?.SetValue(null, ready);
    }

    private static void ResetSettings()
    {
        var settings = new Data.Models.Internal.DbSettings
        {
            Watch = new Data.Models.Internal.DbSettingsWatch
            {
                Interval = 0,
                Default = new Data.Models.Internal.DbSettingsDefaultsWithCategory()
            },
            DownloadClient = new Data.Models.Internal.DbSettingsDownloadClient()
        };

        var property = typeof(SettingData).GetProperty("Get", BindingFlags.Public | BindingFlags.Static);
        property?.SetValue(null, settings);
    }

    private static void ResetPrevCheck(WatchFolderChecker checker)
    {
        var field = typeof(WatchFolderChecker).GetField("_prevCheck", BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(checker, DateTime.MinValue);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldProcessTorrentFile()
    {
        // Arrange
        var filePath = Path.Combine(_testPath, "test.torrent");
        var content = "torrent content"u8.ToArray();
        await File.WriteAllBytesAsync(filePath, content);

        var checker = new WatchFolderChecker(_loggerMock.Object, _serviceProviderMock.Object);
        ResetPrevCheck(checker);

        var cts = new CancellationTokenSource();

        _torrentsServiceMock
            .Setup(x => x.AddFileToDebridQueue(It.IsAny<Byte[]>(), It.IsAny<Torrent>()))
            .ReturnsAsync(new Torrent());

        // Act
        var task = checker.StartAsync(cts.Token);
        await Task.Delay(500); // Give it some time to process
        await cts.CancelAsync();
        try { await task; } catch (OperationCanceledException) { }

        // Assert
        _torrentsServiceMock.Verify(x => x.AddFileToDebridQueue(
            It.Is<Byte[]>(b => b.SequenceEqual(content)),
            It.IsAny<Torrent>()), Times.AtLeastOnce);
        
        Assert.False(File.Exists(filePath));
        Assert.True(File.Exists(Path.Combine(_testPath, "processed", "test.torrent")));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldProcessMagnetFile()
    {
        // Arrange
        var filePath = Path.Combine(_testPath, "test.magnet");
        var content = "magnet:?xt=urn:btih:123";
        await File.WriteAllTextAsync(filePath, content);

        var checker = new WatchFolderChecker(_loggerMock.Object, _serviceProviderMock.Object);
        ResetPrevCheck(checker);

        var cts = new CancellationTokenSource();

        _torrentsServiceMock
            .Setup(x => x.AddMagnetToDebridQueue(It.IsAny<String>(), It.IsAny<Torrent>()))
            .ReturnsAsync(new Torrent());

        // Act
        var task = checker.StartAsync(cts.Token);
        await Task.Delay(500);
        await cts.CancelAsync();
        try { await task; } catch (OperationCanceledException) { }

        // Assert
        _torrentsServiceMock.Verify(x => x.AddMagnetToDebridQueue(
            content,
            It.IsAny<Torrent>()), Times.AtLeastOnce);
        
        Assert.False(File.Exists(filePath));
        Assert.True(File.Exists(Path.Combine(_testPath, "processed", "test.magnet")));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldProcessNzbFile()
    {
        // Arrange
        var filePath = Path.Combine(_testPath, "test.nzb");
        var content = "nzb content"u8.ToArray();
        await File.WriteAllBytesAsync(filePath, content);

        var checker = new WatchFolderChecker(_loggerMock.Object, _serviceProviderMock.Object);
        ResetPrevCheck(checker);

        var cts = new CancellationTokenSource();

        _torrentsServiceMock
            .Setup(x => x.AddNzbFileToDebridQueue(It.IsAny<Byte[]>(), It.IsAny<String>(), It.IsAny<Torrent>()))
            .ReturnsAsync(new Torrent());

        // Act
        var task = checker.StartAsync(cts.Token);
        await Task.Delay(500);
        await cts.CancelAsync();
        try { await task; } catch (OperationCanceledException) { }

        // Assert
        _torrentsServiceMock.Verify(x => x.AddNzbFileToDebridQueue(
            It.Is<Byte[]>(b => b.SequenceEqual(content)),
            "test.nzb",
            It.IsAny<Torrent>()), Times.AtLeastOnce);
        
        Assert.False(File.Exists(filePath));
        Assert.True(File.Exists(Path.Combine(_testPath, "processed", "test.nzb")));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldIgnoreOtherFiles()
    {
        // Arrange
        var filePath = Path.Combine(_testPath, "test.txt");
        await File.WriteAllTextAsync(filePath, "ignore me");

        var checker = new WatchFolderChecker(_loggerMock.Object, _serviceProviderMock.Object);
        ResetPrevCheck(checker);

        var cts = new CancellationTokenSource();

        // Act
        var task = checker.StartAsync(cts.Token);
        await Task.Delay(500);
        await cts.CancelAsync();
        try { await task; } catch (OperationCanceledException) { }

        // Assert
        _torrentsServiceMock.Verify(x => x.AddFileToDebridQueue(It.IsAny<Byte[]>(), It.IsAny<Torrent>()), Times.Never);
        _torrentsServiceMock.Verify(x => x.AddMagnetToDebridQueue(It.IsAny<String>(), It.IsAny<Torrent>()), Times.Never);
        _torrentsServiceMock.Verify(x => x.AddNzbFileToDebridQueue(It.IsAny<Byte[]>(), It.IsAny<String>(), It.IsAny<Torrent>()), Times.Never);
        
        Assert.True(File.Exists(filePath));
    }

    private class MockScopeFactory(IServiceScope scope) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => scope;
    }
}
