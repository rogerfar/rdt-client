using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Moq;
using RdtClient.Service.Services.Downloaders;

namespace RdtClient.Service.Test.Services.Downloaders;

public class StrmDownloaderTest
{
    [Fact]
    public async Task Download_WhenFileCreatedSuccesfully_RaisesDownloadCompleteEvent()
    {
        var mockFileSystem = new MockFileSystem();
        var strmDownloader = new StrmDownloader("http://example.com/video.mp4", "video.mp4", mockFileSystem);

        await Assert.RaisesAsync<DownloadCompleteEventArgs>(a => strmDownloader.DownloadComplete += a,
                                                            a => strmDownloader.DownloadComplete -= a,
                                                            strmDownloader.Download);

        Assert.True(mockFileSystem.FileExists("video.mp4.strm"));
        Assert.Equal(await mockFileSystem.File.ReadAllTextAsync("video.mp4.strm"), "http://example.com/video.mp4");
    }
    
    [Fact]
    public async Task Download_WhenErrorCreatingFile_RaisesDownloadCompleteEventWithError()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<CancellationToken>()))
                      .ThrowsAsync(new IOException("File write error"));
        
        var strmDownloader = new StrmDownloader("http://example.com/video.mp4", "video.mp4", mockFileSystem.Object);
    
        // Act & Assert
        var raisedEvent = await Assert.RaisesAsync<DownloadCompleteEventArgs>(
            a => strmDownloader.DownloadComplete += a,
            a => strmDownloader.DownloadComplete -= a,
            strmDownloader.Download);
    
        Assert.NotNull(raisedEvent);
        Assert.False(String.IsNullOrWhiteSpace(raisedEvent.Arguments.Error));
    }
}
