using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.TorrentClient;
using RdtClient.Service.Helpers;

namespace RdtClient.Service.Test.Helpers;

public class DownloadHelperTest
{
    [Fact]
    public void GetDownloadPath_WithPath_WhenRdNameNull_ReturnsNull()
    {
        // Arrange
        var download = new Download
        {
            Link = "https://fake.url/file.txt",
            FileName = "file.txt"
        };

        var torrent = new Torrent
        {
            RdName = null
        };

        // Act
        var path = DownloadHelper.GetDownloadPath("/data/downloads", torrent, download);

        // Assert
        Assert.Null(path);
    }

    [Fact]
    public void GetDownloadPath_WithoutPath_WhenRdNameNull_ReturnsNull()
    {
        // Arrange
        var download = new Download
        {
            Link = "https://fake.url/file.txt",
            FileName = "file.txt"
        };

        var torrent = new Torrent
        {
            RdName = null
        };

        // Act
        var path = DownloadHelper.GetDownloadPath(torrent, download);

        // Assert
        Assert.Null(path);
    }

    [Fact]
    public void GetDownloadPath_WithPath_WhenDownloadLinkNull_ReturnsNull()
    {
        // Arrange
        var download = new Download
        {
            Link = null,
            FileName = "file.txt"
        };

        var torrent = new Torrent
        {
            RdName = "Torrent Name"
        };

        // Act
        var path = DownloadHelper.GetDownloadPath("/data/downloads", torrent, download);

        // Assert
        Assert.Null(path);
    }

    [Fact]
    public void GetDownloadPath_WithoutPath_WhenDownloadLinkNull_ReturnsNull()
    {
        // Arrange
        var download = new Download
        {
            Link = null,
            FileName = "file.txt"
        };

        var torrent = new Torrent
        {
            RdName = "Torrent Name"
        };

        // Act
        var path = DownloadHelper.GetDownloadPath(torrent, download);

        // Assert
        Assert.Null(path);
    }

    [Fact]
    public void GetDownloadPath_WithPath_WhenDownloadFileNameNull_UsesLinkToGuessFileName()
    {
        // Arrange
        var download = new Download
        {
            Link = "https://fake.url/filename-from-link.txt",
            FileName = null
        };

        var torrent = new Torrent
        {
            RdName = "Torrent Name"
        };

        var fileSystem = new MockFileSystem();

        // Act
        var path = DownloadHelper.GetDownloadPath("/data/downloads", torrent, download, fileSystem);

        // Assert
        var expectedPath = Path.Combine("/data/downloads", torrent.RdName, "filename-from-link.txt");
        Assert.Equal(expectedPath, path);
    }

    [Fact]
    public void GetDownloadPath_WithoutPath_WhenDownloadFileNameNull_UsesLinkToGuessFileName()
    {
        // Arrange
        var download = new Download
        {
            Link = "https://fake.url/filename-from-link.txt",
            FileName = null
        };

        var torrent = new Torrent
        {
            RdName = "Torrent Name"
        };

        // Act
        var path = DownloadHelper.GetDownloadPath(torrent, download);

        // Assert
        var expectedPath = Path.Combine(torrent.RdName, "filename-from-link.txt");
        Assert.Equal(expectedPath, path);
    }

    [Fact]
    public void GetDownloadPath_WithPath_WhenValid_CreatesDirectory()
    {
        // Arrange
        var download = new Download
        {
            Link = "https://fake.url/file.txt",
            FileName = "file.txt"
        };

        var torrent = new Torrent
        {
            RdName = "Torrent Name"
        };

        var fileSystem = new MockFileSystem();

        // Act
        var path = DownloadHelper.GetDownloadPath("/data/downloads", torrent, download, fileSystem);

        // Assert
        var expectedDirectoryPath = Path.Combine("/data/downloads", torrent.RdName);
        Assert.True(fileSystem.Directory.Exists(expectedDirectoryPath));
        var expectedPath = Path.Combine(expectedDirectoryPath, download.FileName);
        Assert.Equal(expectedPath, path);
    }

    [Fact]
    public void GetDownloadPath_WithPath_WhenFileInSubdirectories_ReturnsPathWithSubdirectories()
    {
        // Arrange
        var download = new Download
        {
            Link = "https://fake.url/file.txt",
            FileName = "file.txt"
        };

        var fileRelativePath = "inside/lots/of/subdirectories/file.txt";

        IList<TorrentClientFile> files =
        [
            new()
            {
                Path = fileRelativePath
            }
        ];

        var torrent = new Torrent
        {
            RdName = "Torrent Name",
            RdFiles = JsonSerializer.Serialize(files)
        };

        var fileSystem = new MockFileSystem();

        // Act
        var path = DownloadHelper.GetDownloadPath("/data/downloads", torrent, download, fileSystem);

        // Assert
        var expectedPath = Path.Combine("/data/downloads", torrent.RdName, fileRelativePath);
        Assert.Equal(expectedPath, path);
    }

    [Fact]
    public void GetDownloadPath_WithoutPath_WhenFileInSubdirectories_ReturnsPathWithSubdirectories()
    {
        // Arrange
        var download = new Download
        {
            Link = "https://fake.url/file.txt",
            FileName = "file.txt"
        };

        var fileRelativePath = "inside/lots/of/subdirectories/file.txt";

        IList<TorrentClientFile> files =
        [
            new()
            {
                Path = fileRelativePath
            }
        ];

        var torrent = new Torrent
        {
            RdName = "Torrent Name",
            RdFiles = JsonSerializer.Serialize(files)
        };

        // Act
        var path = DownloadHelper.GetDownloadPath(torrent, download);

        // Assert
        var expectedPath = Path.Combine(torrent.RdName, fileRelativePath);
        Assert.Equal(expectedPath, path);
    }

    // This is probably a bug
    [Fact]
    public void GetDownloadPath_WithPath_WhenNoUriSegmentsOrFileName_ReturnsTorrentDirectory()
    {
        // Arrange
        var download = new Download
        {
            Link = "https://fake.url", // HttpUtility.UrlDecode(new Uri("https://fake.url").Segments.Last()) == "/"
            FileName = null
        };

        var torrent = new Torrent
        {
            RdName = "Torrent Name"
        };

        var fileSystem = new MockFileSystem();

        // Act
        var path = DownloadHelper.GetDownloadPath("/data/downloads", torrent, download, fileSystem);

        // Assert
        var expectedPath = Path.Combine("/data/downloads", torrent.RdName);
        Assert.Equal(expectedPath, path);
    }
}
