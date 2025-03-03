using Microsoft.Extensions.Logging;
using Moq;
using RdtClient.Data.Models.Data;
using RdtClient.Service.Services;

namespace RdtClient.Service.Test.Services;

public class DownloadableFileFilterTest
{
    [Fact]
    public void IsDownloadable_WhenNoFilterSpecified_ReturnsTrue()
    {
        // Arrange
        var mocks = new Mocks();

        var torrent = new Torrent
        {
            RdId = "1"
        };
        
        var fileFilter = new DownloadableFileFilter(mocks.LoggerMock.Object);

        // Act
        var result = fileFilter.IsDownloadable(torrent, "file.txt", 10000);

        // Assert
        Assert.True(result);
    }

    [Theory]
    // downloadMinSize is in MB, fileSize is in B
    [InlineData(100, 20 * 1024 * 1024)]
    [InlineData(2,   2 * 1024 * 1024)]
    [InlineData(2,   2 * (1000 * 1000 + 1))] // mostly to show we use 1024 not 1000 for conversion
    public void IsDownloadable_WhenDownloadMinSizeSpecified_AndDownloadBelowSize_ReturnsFalse(Int32 downloadMinSize, Int64 fileSize)
    {
        // Arrange
        var mocks = new Mocks();

        var torrent = new Torrent
        {
            RdId = "1",
            DownloadMinSize = downloadMinSize
        };

        var fileFilter = new DownloadableFileFilter(mocks.LoggerMock.Object);

        // Act
        var result = fileFilter.IsDownloadable(torrent, "file.txt", fileSize);

        // Assert
        Assert.False(result);
    }
    
    [Theory]
    [InlineData(100, 110 * 1024 * 1024)]
    [InlineData(2,   2 * 1024 * 1024 + 1)]
    public void IsDownloadable_WhenDownloadMinSizeSpecified_AndDownloadAboveSize_ReturnsTrue(Int32 downloadMinSize, Int64 fileSize)
    {
        // Arrange
        var mocks = new Mocks();

        var torrent = new Torrent
        {
            RdId = "1",
            DownloadMinSize = downloadMinSize
        };

        var fileFilter = new DownloadableFileFilter(mocks.LoggerMock.Object);

        // Act
        var result = fileFilter.IsDownloadable(torrent, "file.txt", fileSize);

        // Assert
        Assert.True(result);
    }
    
    [Theory]
    [InlineData("file", "no-match")]
    [InlineData("file", "even/in/a/subdirectory.txt")]
    [InlineData("ch[aA]racter c[lL]asses", "nope.txt")]
    [InlineData("digits\\d+", "123 not matching.txt")]
    public void IsDownloadable_WhenIncludeRegexSpecified_AndPathDoesNotMatchRegex_ReturnsFalse(String includeRegex, String filePath)
    {
        // Arrange
        var mocks = new Mocks();

        var torrent = new Torrent
        {
            RdId = "1",
            IncludeRegex = includeRegex
        };

        var fileFilter = new DownloadableFileFilter(mocks.LoggerMock.Object);

        // Act
        var result = fileFilter.IsDownloadable(torrent, filePath, Int64.MaxValue);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("file", "file.txt")]
    [InlineData("file", "file/in/a/subdirectory.txt")]
    [InlineData("ch[aA]racter c[lL]asses", "character cLasses")]
    [InlineData("digits\\d+", "digits123456.txt")]
    public void IsDownloadable_WhenIncludeRegexSpecified_AndPathMatchesRegex_ReturnsTrue(String includeRegex, String filePath)
    {
        // Arrange
        var mocks = new Mocks();

        var torrent = new Torrent
        {
            RdId = "1",
            IncludeRegex = includeRegex
        };

        var fileFilter = new DownloadableFileFilter(mocks.LoggerMock.Object);

        // Act
        var result = fileFilter.IsDownloadable(torrent, filePath, Int64.MaxValue);

        // Assert
        Assert.True(result);
    }
    
    [Theory]
    [InlineData("file", "no-match")]
    [InlineData("file", "even/in/a/subdirectory.txt")]
    [InlineData("ch[aA]racter c[lL]asses", "nope.txt")]
    [InlineData("digits\\d+", "123 not matching.txt")]
    public void IsDownloadable_WhenExcludeRegexSpecified_AndPathDoesNotMatchRegex_ReturnsTrue(String excludeRegex, String filePath)
    {
        // Arrange
        var mocks = new Mocks();

        var torrent = new Torrent
        {
            RdId = "1",
            ExcludeRegex = excludeRegex
        };

        var fileFilter = new DownloadableFileFilter(mocks.LoggerMock.Object);

        // Act
        var result = fileFilter.IsDownloadable(torrent, filePath, Int64.MaxValue);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("file", "file.txt")]
    [InlineData("file", "file/in/a/subdirectory.txt")]
    [InlineData("ch[aA]racter c[lL]asses", "character cLasses")]
    [InlineData("digits\\d+", "digits123456.txt")]
    public void IsDownloadable_WhenExcludeRegexSpecified_AndPathMatchesRegex_ReturnsFalse(String excludeRegex, String filePath)
    {
        // Arrange
        var mocks = new Mocks();

        var torrent = new Torrent
        {
            RdId = "1",
            ExcludeRegex = excludeRegex
        };

        var fileFilter = new DownloadableFileFilter(mocks.LoggerMock.Object);

        // Act
        var result = fileFilter.IsDownloadable(torrent, filePath, Int64.MaxValue);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("file", "file", "file.txt")]
    [InlineData("file", "in/a", "file/in/a/subdirectory.txt")]
    [InlineData("ch[aA]racter c[lL]asses", "character", "character cLasses")]
    [InlineData("digits\\d+", "123456", "digits123456.txt")]
    public void IsDownloadable_WhenBothIncludeAndExcludeRegexSpecified_AndPathMatchesIncludeAndExcludeRegex_ReturnsTrue(String includeRegex, String excludeRegex, String filePath)
    {
        // Arrange
        var mocks = new Mocks();

        var torrent = new Torrent
        {
            RdId = "1",
            IncludeRegex = includeRegex,
            ExcludeRegex = excludeRegex
        };

        var fileFilter = new DownloadableFileFilter(mocks.LoggerMock.Object);

        // Act
        var result = fileFilter.IsDownloadable(torrent, filePath, Int64.MaxValue);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(10, "file", 10 * 1024 * 1024 + 1, "no-match.txt")]
    public void IsDownloadable_WhenBothDownloadMinSizeAndIncludeRegexSpecified_AndDownloadAboveSizeAndDoesNotMatchRegex_ReturnsFalse(
        Int32 minSize,
        String includeRegex,
        Int64 fileSize,
        String filePath)
    {
        // Arrange
        var mocks = new Mocks();
        
        var torrent = new Torrent
        {
            RdId = "1",
            IncludeRegex = includeRegex,
            DownloadMinSize = minSize
        };
        
        var fileFilter = new DownloadableFileFilter(mocks.LoggerMock.Object);

        // Act
        var result = fileFilter.IsDownloadable(torrent, filePath, fileSize);

        // Assert
        Assert.False(result);
    }
    
    [Theory]
    [InlineData(10, "file", 10 * 1024 * 1024 - 1, "file.txt")]
    public void IsDownloadable_WhenBothDownloadMinSizeAndIncludeRegexSpecified_AndDownloadBelowSizeAndMatchesRegex_ReturnsFalse(
        Int32 minSize,
        String includeRegex,
        Int64 fileSize,
        String filePath)
    {
        // Arrange
        var mocks = new Mocks();
        
        var torrent = new Torrent
        {
            RdId = "1",
            IncludeRegex = includeRegex,
            DownloadMinSize = minSize
        };
        
        var fileFilter = new DownloadableFileFilter(mocks.LoggerMock.Object);

        // Act
        var result = fileFilter.IsDownloadable(torrent, filePath, fileSize);

        // Assert
        Assert.False(result);
    }
    private class Mocks
    {
        public readonly Mock<ILogger<DownloadableFileFilter>> LoggerMock = new();
    }
}