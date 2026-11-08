using System.Text;
using RdtClient.Service.Helpers;

namespace RdtClient.Service.Test.Helpers;

public class DirectDownloadHelperTest
{
    private static Byte[] StandInNzb(String post, String name = "Show.S01E01.1080p.WEB.H264-GRP")
    {
        return Encoding.UTF8.GetBytes($"""
                                       <?xml version="1.0" encoding="UTF-8"?>
                                       <nzb xmlns="http://www.newzbin.com/DTD/2003/nzb">
                                       <head>
                                       <meta type="name">{name}</meta>
                                       <meta type="x-ddl-source">ddl-site</meta>
                                       <meta type="x-ddl-post">{post}</meta>
                                       </head>
                                       <file poster="bridge" date="1" subject="placeholder"><groups><group>alt.binaries.ddl</group></groups>
                                       <segments><segment bytes="1" number="1">x@ddl.invalid</segment></segments></file>
                                       </nzb>
                                       """);
    }

    [Fact]
    public void ParseStandIn_ReadsPostTitleAndSource()
    {
        var result = DirectDownloadHelper.ParseStandIn(StandInNzb("https://ddl-site.example/show-s01e01-1080p-web-h264-grp-1-1-gb/", "Show &amp; Co.S01E01"));

        Assert.NotNull(result);
        Assert.Equal("https://ddl-site.example/show-s01e01-1080p-web-h264-grp-1-1-gb/", result.PostUrl);
        Assert.Equal("Show & Co.S01E01", result.Title);
        Assert.Equal("ddl-site", result.Source);
    }

    [Fact]
    public void ParseStandIn_ReturnsNullForRegularNzb()
    {
        var nzb = Encoding.UTF8.GetBytes("""
                                         <?xml version="1.0" encoding="UTF-8"?>
                                         <nzb xmlns="http://www.newzbin.com/DTD/2003/nzb"><head><meta type="name">Real.Release</meta></head>
                                         <file poster="p" date="1" subject="s"><groups><group>g</group></groups><segments><segment bytes="1" number="1">a@b</segment></segments></file></nzb>
                                         """);

        Assert.Null(DirectDownloadHelper.ParseStandIn(nzb));
    }

    [Theory]
    [InlineData("not xml at all")]
    [InlineData("")]
    public void ParseStandIn_ReturnsNullForGarbage(String content)
    {
        Assert.Null(DirectDownloadHelper.ParseStandIn(Encoding.UTF8.GetBytes(content)));
    }

    [Fact]
    public void ParseStandIn_RejectsNonHttpPost()
    {
        Assert.Null(DirectDownloadHelper.ParseStandIn(StandInNzb("file:///etc/passwd")));
    }

    [Fact]
    public void PickMirror_KeepsOneHostWhenMirrorsHaveTheSameParts()
    {
        const String text = """
                            https://nitroflare.com/view/AAA/Movie.2026.1080p-GRP.part1.rar
                            https://nitroflare.com/view/BBB/Movie.2026.1080p-GRP.part2.rar
                            https://rapidgator.net/file/ccc/Movie.2026.1080p-GRP.part1.rar.html
                            https://rapidgator.net/file/ddd/Movie.2026.1080p-GRP.part2.rar.html
                            """;

        var links = DirectDownloadHelper.PickMirror(text);

        Assert.Equal(2, links.Count);
        Assert.All(links, l => Assert.Contains("rapidgator.net", l));
    }

    [Fact]
    public void PickMirror_SkipsAPreferredHostMissingAnArchivePart()
    {
        const String text = "https://rg.to/file/a/x.part1.rar.html, see also https://nitroflare.com/view/B/x.part1.rar https://nitroflare.com/view/C/x.part2.rar.";

        var links = DirectDownloadHelper.PickMirror(text);

        Assert.Equal(["https://nitroflare.com/view/B/x.part1.rar", "https://nitroflare.com/view/C/x.part2.rar"], links);
    }

    [Fact]
    public void PickMirror_ConfiguredPreferenceBreaksTies()
    {
        const String text = "https://rapidgator.net/file/a/x.mkv.html https://nitroflare.com/view/B/x.mkv";

        Assert.Equal(["https://nitroflare.com/view/B/x.mkv"], DirectDownloadHelper.PickMirror(text, "nitroflare, rapidgator"));
        Assert.Equal(["https://rapidgator.net/file/a/x.mkv.html"], DirectDownloadHelper.PickMirror(text, "rg.to"));
    }

    [Fact]
    public void PickMirror_IncompleteArchiveSetLosesEvenWhenPreferred()
    {
        const String text = "https://rapidgator.net/file/a/x.part1.rar.html https://rapidgator.net/file/b/x.part2.rar.html https://nitroflare.com/view/C/x.part1.rar";

        Assert.All(DirectDownloadHelper.PickMirror(text, "nitroflare"), l => Assert.Contains("rapidgator.net", l));
    }

    // One host offers the whole file, the other the same release split in two archives.
    private const String WholeFileOrArchives = """
                                               https://rapidgator.net/file/aaa/Movie.2026.2160p.WEB.h265-GRP.mkv
                                               https://nitroflare.com/view/BBB/Movie.2026.2160p.WEB.h265-GRP.part1.rar
                                               https://nitroflare.com/view/CCC/Movie.2026.2160p.WEB.h265-GRP.part2.rar
                                               """;

    [Fact]
    public void PlanParts_PreferenceBeatsLinkCountWhenHostsPackageTheReleaseDifferently()
    {
        var parts = DirectDownloadHelper.PlanParts(WholeFileOrArchives);

        var part = Assert.Single(parts);
        Assert.Equal("https://rapidgator.net/file/aaa/Movie.2026.2160p.WEB.h265-GRP.mkv", part.Link);
        Assert.Empty(part.Mirrors);
    }

    [Fact]
    public void FallbackLinks_KeepsTheOtherHostsAndPlanningThemPicksTheNextHost()
    {
        var fallback = DirectDownloadHelper.FallbackLinks(WholeFileOrArchives);

        Assert.Equal("https://nitroflare.com/view/BBB/Movie.2026.2160p.WEB.h265-GRP.part1.rar\n" +
                     "https://nitroflare.com/view/CCC/Movie.2026.2160p.WEB.h265-GRP.part2.rar",
                     fallback);
        Assert.All(DirectDownloadHelper.PlanParts(fallback!), p => Assert.Contains("nitroflare.com", p.Link));
        Assert.Null(DirectDownloadHelper.FallbackLinks(fallback!));
    }

    [Theory]
    [InlineData("Host under maintenance or not available (LINK_HOST_UNAVAILABLE)", true)]
    [InlineData("This host or link is not supported (LINK_HOST_NOT_SUPPORTED)", true)]
    [InlineData("Link is dead (LINK_DOWN)", false)]
    [InlineData(null, false)]
    public void IsHostRejection(String? error, Boolean expected)
    {
        Assert.Equal(expected, DirectDownloadHelper.IsHostRejection(error));
    }

    [Theory]
    [InlineData("Host under maintenance or not available (LINK_HOST_UNAVAILABLE)", true)]
    [InlineData("Too many concurrent downloads for this host (LINK_TOO_MANY_DOWNLOADS)", true)]
    [InlineData("Response status code does not indicate success: 503 (unavailable).", true)]
    [InlineData("Response status code does not indicate success: 404 (Not Found).", false)]
    [InlineData("This link is not available on the file hoster website (LINK_DOWN)", false)]
    [InlineData("This host or link is not supported (LINK_HOST_NOT_SUPPORTED)", false)]
    [InlineData(null, false)]
    public void IsTemporaryError(String? error, Boolean expected)
    {
        Assert.Equal(expected, DirectDownloadHelper.IsTemporaryError(error));
    }

    [Fact]
    public void HostPreference_NormalizesDomainsAndAliases()
    {
        Assert.Equal(["nitroflare", "rapidgator", "1fichier"], DirectDownloadHelper.HostPreference("nitro.download;rapidgator.net rg 1fichier"));
    }

    [Fact]
    public void PickMirror_DeduplicatesAndReturnsEmptyWithoutLinks()
    {
        Assert.Single(DirectDownloadHelper.PickMirror("https://rapidgator.net/file/a/x.rar.html https://rapidgator.net/file/a/x.rar.html"));
        Assert.Empty(DirectDownloadHelper.PickMirror("no links here"));
    }

    [Fact]
    public void PlanParts_NeverUsesABlacklistedHostUnlessItIsTheOnlyOne()
    {
        const String text = """
                            https://nitroflare.com/view/C/Movie.2026.1080p-GRP.part1.rar
                            https://nitroflare.com/view/D/Movie.2026.1080p-GRP.part2.rar
                            https://ddownload.com/e/Movie.2026.1080p-GRP.part1.rar
                            https://ddownload.com/f/Movie.2026.1080p-GRP.part2.rar
                            """;

        var parts = DirectDownloadHelper.PlanParts(text, "nitroflare,ddownload", "nitroflare.com");

        Assert.Equal(["https://ddownload.com/e/Movie.2026.1080p-GRP.part1.rar", "https://ddownload.com/f/Movie.2026.1080p-GRP.part2.rar"],
                     parts.Select(p => p.Link));
        Assert.All(parts, p => Assert.Empty(p.Mirrors));
        Assert.Null(DirectDownloadHelper.FallbackLinks(text, "nitroflare,ddownload", "nitroflare"));

        var onlyBlacklisted = DirectDownloadHelper.PlanParts("https://nitroflare.com/view/C/Movie.2026.1080p-GRP.mkv", null, "nitroflare");
        Assert.Equal("https://nitroflare.com/view/C/Movie.2026.1080p-GRP.mkv", Assert.Single(onlyBlacklisted).Link);
    }

    [Theory]
    [InlineData("Too many concurrent downloads for this host (LINK_TOO_MANY_DOWNLOADS)", true)]
    [InlineData("Host under maintenance or not available (LINK_HOST_UNAVAILABLE)", false)]
    [InlineData(null, false)]
    public void IsHostBusy(String? error, Boolean expected)
    {
        Assert.Equal(expected, DirectDownloadHelper.IsHostBusy(error));
    }

    [Fact]
    public void PlanParts_KeepsTheSameFileOnOtherHostsAsMirrors()
    {
        const String text = """
                            https://rapidgator.net/file/a/Movie.2026.1080p-GRP.part1.rar.html
                            https://rapidgator.net/file/b/Movie.2026.1080p-GRP.part2.rar.html
                            https://nitroflare.com/view/C/Movie.2026.1080p-GRP.part1.rar
                            https://nitroflare.com/view/D/Movie.2026.1080p-GRP.part2.rar
                            https://ddownload.com/e/Movie.2026.1080p-GRP.part2.rar
                            """;

        var parts = DirectDownloadHelper.PlanParts(text);

        Assert.Equal(["https://rapidgator.net/file/a/Movie.2026.1080p-GRP.part1.rar.html", "https://rapidgator.net/file/b/Movie.2026.1080p-GRP.part2.rar.html"],
                     parts.Select(p => p.Link));
        Assert.Equal(["https://nitroflare.com/view/C/Movie.2026.1080p-GRP.part1.rar"], parts[0].Mirrors);
        Assert.Equal(["https://nitroflare.com/view/D/Movie.2026.1080p-GRP.part2.rar", "https://ddownload.com/e/Movie.2026.1080p-GRP.part2.rar"], parts[1].Mirrors);
    }

    [Fact]
    public void PlanParts_LinksWithoutAFileNameGetNoMirrors()
    {
        var parts = DirectDownloadHelper.PlanParts("https://rapidgator.net/file/abc123 https://nitroflare.com/view/XYZ");

        Assert.Single(parts);
        Assert.Empty(parts[0].Mirrors);
    }

    [Theory]
    [InlineData("https://rapidgator.net/file/a/My%20Movie.part1.rar.html", "my movie.part1.rar")]
    [InlineData("https://nitroflare.com/view/B/My%20Movie.part1.rar", "my movie.part1.rar")]
    [InlineData("https://nitroflare.com/view/B/", null)]
    [InlineData("https://rapidgator.net/file/abc123", null)]
    public void MirrorKey(String link, String? expected)
    {
        Assert.Equal(expected, DirectDownloadHelper.MirrorKey(link));
    }

    [Theory]
    [InlineData("Movie.part1.rar", false)]
    [InlineData("Movie.part01.rar", false)]
    [InlineData("Movie.part2.rar", true)]
    [InlineData("Movie.part10.rar", true)]
    [InlineData("Movie.rar", false)]
    [InlineData("Movie.r00", true)]
    [InlineData("Movie.z01", true)]
    [InlineData("Movie.mkv", false)]
    [InlineData(null, false)]
    public void IsSecondaryArchiveVolume(String? fileName, Boolean expected)
    {
        Assert.Equal(expected, DirectDownloadHelper.IsSecondaryArchiveVolume(fileName));
    }

    [Fact]
    public void FileNameFromLink_DecodesTheLastSegment()
    {
        Assert.Equal("My Movie.part1.rar", DirectDownloadHelper.FileNameFromLink("https://abc.debrid.it/dl/xyz/My%20Movie.part1.rar"));
    }

    [Fact]
    public void Hash_IsStableLowercaseSha1()
    {
        var hash = DirectDownloadHelper.Hash("https://ddl-site.example/post/");

        Assert.Equal(40, hash.Length);
        Assert.Equal(hash.ToLowerInvariant(), hash);
        Assert.Equal(hash, DirectDownloadHelper.Hash(" https://ddl-site.example/post/ "));
    }

    [Fact]
    public void LinksHash_IgnoresOrderAndSurroundingText()
    {
        var a = DirectDownloadHelper.LinksHash("https://rapidgator.net/file/a/x.part1.rar.html\nhttps://rapidgator.net/file/b/x.part2.rar.html");
        var b = DirectDownloadHelper.LinksHash("Mirror:\nhttps://rapidgator.net/file/b/x.part2.rar.html, https://rapidgator.net/file/a/x.part1.rar.html");

        Assert.Equal(a, b);
        Assert.NotEqual(a, DirectDownloadHelper.LinksHash("https://rapidgator.net/file/a/x.part1.rar.html"));
    }

    [Theory]
    [InlineData("https://rapidgator.net/file/abc/Movie.2026.1080p-GRP.part01.rar.html", "Movie.2026.1080p-GRP")]
    [InlineData("https://nitroflare.com/view/ABC/Movie.2026.1080p-GRP.part2.rar", "Movie.2026.1080p-GRP")]
    [InlineData("https://nitroflare.com/view/ABC/Movie.2026.1080p-GRP.mkv", "Movie.2026.1080p-GRP")]
    [InlineData("https://ddownload.com/abc123", "ddownload")]
    public void ReleaseName_StripsHosterAndArchiveSuffixes(String link, String expected)
    {
        Assert.Equal(expected, DirectDownloadHelper.ReleaseName(link));
    }
}
