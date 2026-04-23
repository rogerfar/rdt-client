using RdtClient.Service.Helpers;
using RdtClient.Service.Services;

namespace RdtClient.Service.Test.Helpers;

public class FilenameSanitizerTest
{
    [Theory]
    [InlineData(
        "Fixer.Upper.S04E11.Space.in.the.Suburbs.720p.HDTV.x264-W4F[eztv].mkv",
        "Fixer.Upper.S04E11.Space.in.the.Suburbs.720p.HDTV.x264-W4Feztv.mkv")]
    [InlineData(
        "The.Bear.S03E01.Tomorrow.1080p.DSNP.WEB-DL.DDP5.1.H.264-NTb[EZTVx.to].mkv",
        "The.Bear.S03E01.Tomorrow.1080p.DSNP.WEB-DL.DDP5.1.H.264-NTbEZTVx.to.mkv")]
    public void SanitizeFilename_StripsSquareBrackets(String input, String expected)
    {
        var result = FilenameSanitizer.SanitizeFilename(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SanitizeFilename_StripsCurlyBraces()
    {
        var result = FilenameSanitizer.SanitizeFilename("Movie{2024}.mkv");
        Assert.Equal("Movie2024.mkv", result);
    }

    [Theory]
    [InlineData("Movie  Name  2024.mkv", "Movie Name 2024.mkv")]
    [InlineData("Too    Many     Spaces.mkv", "Too Many Spaces.mkv")]
    public void SanitizeFilename_CollapsesMultipleSpaces(String input, String expected)
    {
        var result = FilenameSanitizer.SanitizeFilename(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(" Leading.mkv", "Leading.mkv")]
    [InlineData("Trailing.mkv ", "Trailing.mkv")]
    [InlineData("  Both  Sides  ", "Both Sides")]
    public void SanitizeFilename_TrimsWhitespace(String input, String expected)
    {
        var result = FilenameSanitizer.SanitizeFilename(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    // Uses explicit \u escapes so the control characters survive editors,
    // diffs, and CI log copy-paste. Each case verifies a different range
    // of Unicode control characters.
    [InlineData("Movie\u0001Name\u001F.mkv", "MovieName.mkv")] // C0 range (0x00-0x1F)
    [InlineData("Movie\tName.mkv",             "MovieName.mkv")] // tab
    [InlineData("Movie\u007FName.mkv",         "MovieName.mkv")] // DEL (0x7F)
    [InlineData("Movie\u0080Name\u009F.mkv", "MovieName.mkv")] // C1 range (0x80-0x9F)
    public void SanitizeFilename_StripsControlCharacters(String input, String expected)
    {
        var result = FilenameSanitizer.SanitizeFilename(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SanitizeFilename_LeavesCleanFilenamesUnchanged()
    {
        const String clean = "fixer.upper.s05e17.chip.and.jos.breakfast.joint.hdtv.x264-w4f.mkv";
        var result = FilenameSanitizer.SanitizeFilename(clean);
        Assert.Equal(clean, result);
    }

    [Fact]
    public void SanitizeFilename_PreservesParentheses()
    {
        const String input = "Movie (2024) 1080p.mkv";
        var result = FilenameSanitizer.SanitizeFilename(input);
        Assert.Equal(input, result);
    }

    [Fact]
    public void SanitizeFilename_ReturnsEmptyForEmpty()
    {
        var result = FilenameSanitizer.SanitizeFilename("");
        Assert.Equal("", result);
    }

    [Fact]
    public void SanitizeFilename_ReturnsEmptyForNull()
    {
        var result = FilenameSanitizer.SanitizeFilename(null);
        Assert.Equal("", result);
    }

    [Fact]
    public void SanitizeFilename_CombinedIssues()
    {
        var result = FilenameSanitizer.SanitizeFilename("Some  Show  S01E01  720p  HDTV  [eztv].mkv");
        Assert.Equal("Some Show S01E01 720p HDTV eztv.mkv", result);
    }

    [Fact]
    public void SanitizeFilename_StripsBracketsEvenWhenSpacedApart()
    {
        var result = FilenameSanitizer.SanitizeFilename("Some Show [2024] 1080p.mkv");
        Assert.Equal("Some Show 2024 1080p.mkv", result);
    }

    [Fact]
    public void SanitizePath_SanitizesFilenameSegment()
    {
        var path = Path.Combine(Path.DirectorySeparatorChar + "data", "downloads", "tv-sonarr", "Fixer.Upper.S04E11[eztv].mkv");
        var expected = Path.Combine(Path.DirectorySeparatorChar + "data", "downloads", "tv-sonarr", "Fixer.Upper.S04E11eztv.mkv");

        var result = FilenameSanitizer.SanitizePath(path);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void SanitizePath_SanitizesDirectoryNames()
    {
        var path = Path.Combine(Path.DirectorySeparatorChar + "data", "downloads", "Some  Show [2024]", "Episode[eztv].mkv");
        var expected = Path.Combine(Path.DirectorySeparatorChar + "data", "downloads", "Some Show 2024", "Episodeeztv.mkv");

        var result = FilenameSanitizer.SanitizePath(path);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void SanitizePath_PreservesDirectorySeparators()
    {
        var path = Path.Combine(Path.DirectorySeparatorChar + "data", "downloads", "clean", "file.mkv");

        var result = FilenameSanitizer.SanitizePath(path);

        Assert.Equal(path, result);
    }

    [Fact]
    public void SanitizePath_HandlesMixedSeparators()
    {
        var result = FilenameSanitizer.SanitizePath("data/downloads\\Some Show [2024]/Episode[eztv].mkv");
        var expected = String.Join(Path.DirectorySeparatorChar, "data", "downloads", "Some Show 2024", "Episodeeztv.mkv");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void SanitizePath_ReturnsEmptyForEmpty()
    {
        Assert.Equal("", FilenameSanitizer.SanitizePath(""));
    }

    [Fact]
    public void SanitizePath_ReturnsEmptyForNull()
    {
        Assert.Equal("", FilenameSanitizer.SanitizePath(null));
    }

    [Fact]
    public void SanitizeFilenameIfEnabled_ReturnsUnchangedWhenDisabled()
    {
        var original = Settings.Get.DownloadClient.SanitizeFilenames;
        try
        {
            Settings.Get.DownloadClient.SanitizeFilenames = false;

            const String input = "Movie  [eztv].mkv";
            Assert.Equal(input, FilenameSanitizer.SanitizeFilenameIfEnabled(input));
        }
        finally
        {
            Settings.Get.DownloadClient.SanitizeFilenames = original;
        }
    }

    [Fact]
    public void SanitizeFilenameIfEnabled_SanitizesWhenEnabled()
    {
        var original = Settings.Get.DownloadClient.SanitizeFilenames;
        try
        {
            Settings.Get.DownloadClient.SanitizeFilenames = true;

            Assert.Equal("Movie eztv.mkv", FilenameSanitizer.SanitizeFilenameIfEnabled("Movie  [eztv].mkv"));
        }
        finally
        {
            Settings.Get.DownloadClient.SanitizeFilenames = original;
        }
    }

    [Fact]
    public void SanitizePathIfEnabled_ReturnsUnchangedWhenDisabled()
    {
        var original = Settings.Get.DownloadClient.SanitizeFilenames;
        try
        {
            Settings.Get.DownloadClient.SanitizeFilenames = false;

            var input = Path.Combine(Path.DirectorySeparatorChar + "data", "downloads", "Show [2024]", "Episode[eztv].mkv");
            Assert.Equal(input, FilenameSanitizer.SanitizePathIfEnabled(input));
        }
        finally
        {
            Settings.Get.DownloadClient.SanitizeFilenames = original;
        }
    }
}
