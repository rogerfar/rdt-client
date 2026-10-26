using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.DebridClient;
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

        var fileRelativePath = Path.Combine("inside", "lots", "of", "subdirectories", "file.txt");

        IList<DebridClientFile> files =
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
        var expectedPath = Path.Combine("/data/downloads", torrent.RdName, "inside", "lots", "of", "subdirectories", "file.txt");
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

        var fileRelativePath = Path.Combine("inside", "lots", "of", "subdirectories", "file.txt");

        IList<DebridClientFile> files =
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
        var expectedPath = Path.Combine(torrent.RdName, "inside", "lots", "of", "subdirectories", "file.txt");
        Assert.Equal(expectedPath, path);
    }

    [Fact]
    public void GetDownloadPath_WithPath_WhenFilePathStartsWithTorrentName_StripsPrefix()
    {
        // Arrange
        var download = new Download
        {
            Link = "https://fake.url/file.txt",
            FileName = "file.txt"
        };

        var fileRelativePath = Path.Combine("Torrent Name", "Saison 1", "file.txt");

        IList<DebridClientFile> files =
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
        // The torrent name prefix in the file path should not duplicate the torrent name in the base dir
        var expectedPath = Path.Combine("/data/downloads", "Torrent Name", "Saison 1", "file.txt");
        Assert.Equal(expectedPath, path);
    }

    [Fact]
    public void GetDownloadPath_WithoutPath_WhenFilePathStartsWithTorrentName_StripsPrefix()
    {
        // Arrange
        var download = new Download
        {
            Link = "https://fake.url/file.txt",
            FileName = "file.txt"
        };

        var fileRelativePath = Path.Combine("Torrent Name", "Saison 1", "file.txt");

        IList<DebridClientFile> files =
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
        var expectedPath = Path.Combine("Torrent Name", "Saison 1", "file.txt");
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

    // Audiobook packs reuse names like "Track 001.mp3" in every folder. TorBox's restricted link ends in the file id,
    // which tells the files apart.
    private static Torrent TorrentWithSameNameInTwoFolders()
    {
        IList<DebridClientFile> files =
        [
            new()
            {
                Id = 3,
                Path = "Book 1/Track 001.mp3",
                Bytes = 100
            },
            new()
            {
                Id = 7,
                Path = "Book 2/Track 001.mp3",
                Bytes = 200
            }
        ];

        return new()
        {
            RdName = "Torrent Name",
            RdFiles = JsonSerializer.Serialize(files)
        };
    }

    private static Download DownloadOfFileId(Int64 fileId)
    {
        return new()
        {
            Path = $"https://torbox.app/fakedl/100/{fileId}",
            Link = "https://cdn.torbox.app/abc/Track%20001.mp3",
            FileName = "Track 001.mp3"
        };
    }

    [Fact]
    public void GetDownloadPath_WithPath_WhenSameFileNameInSeveralFolders_UsesFolderOfFileId()
    {
        var torrent = TorrentWithSameNameInTwoFolders();

        var path = DownloadHelper.GetDownloadPath("/data/downloads", torrent, DownloadOfFileId(7), new MockFileSystem());

        Assert.Equal(Path.Combine("/data/downloads", "Torrent Name", "Book 2", "Track 001.mp3"), path);
    }

    [Fact]
    public void GetDownloadPath_WithoutPath_WhenSameFileNameInSeveralFolders_UsesFolderOfFileId()
    {
        var torrent = TorrentWithSameNameInTwoFolders();

        var path = DownloadHelper.GetDownloadPath(torrent, DownloadOfFileId(7));

        Assert.Equal(Path.Combine("Torrent Name", "Book 2", "Track 001.mp3"), path);
    }

    // AllDebrid and DebridLink store each file's restricted link on the file itself.
    [Fact]
    public void GetDownloadPath_WithPath_WhenSameFileNameInSeveralFolders_UsesFolderOfFileWithSameDownloadLink()
    {
        IList<DebridClientFile> files =
        [
            new()
            {
                Path = "Disc 1/Subs/English.srt",
                DownloadLink = "https://alldebrid.com/f/aaa"
            },
            new()
            {
                Path = "Disc 2/Subs/English.srt",
                DownloadLink = "https://alldebrid.com/f/bbb"
            }
        ];

        var torrent = new Torrent
        {
            RdName = "Torrent Name",
            RdFiles = JsonSerializer.Serialize(files)
        };

        var download = new Download
        {
            Path = "https://alldebrid.com/f/bbb",
            Link = "https://cdn.alldebrid.com/xyz/English.srt",
            FileName = "English.srt"
        };

        var path = DownloadHelper.GetDownloadPath("/data/downloads", torrent, download, new MockFileSystem());

        Assert.Equal(Path.Combine("/data/downloads", "Torrent Name", "Disc 2", "Subs", "English.srt"), path);
    }

    [Fact]
    public void GetDownloadPath_WithPath_WhenSameFileNameAndNoIdInLink_KeepsFirstMatch()
    {
        var torrent = TorrentWithSameNameInTwoFolders();

        var download = new Download
        {
            Path = "https://real-debrid.com/d/ABCDEF",
            Link = "https://cdn.real-debrid.com/abc/Track%20001.mp3",
            FileName = "Track 001.mp3"
        };

        var path = DownloadHelper.GetDownloadPath("/data/downloads", torrent, download, new MockFileSystem());

        Assert.Equal(Path.Combine("/data/downloads", "Torrent Name", "Book 1", "Track 001.mp3"), path);
    }

    [Fact]
    public void GetExpectedFileSize_WhenSameFileNameInSeveralFolders_UsesSizeOfFileId()
    {
        var torrent = TorrentWithSameNameInTwoFolders();

        Assert.Equal(200, DownloadHelper.GetExpectedFileSize(torrent, DownloadOfFileId(7)));
        Assert.Equal(100, DownloadHelper.GetExpectedFileSize(torrent, DownloadOfFileId(3)));
    }
}
