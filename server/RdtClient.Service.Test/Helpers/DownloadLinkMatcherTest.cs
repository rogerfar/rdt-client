using RdtClient.Data.Models.Data;
using RdtClient.Service.Helpers;

namespace RdtClient.Service.Test.Helpers;

public class DownloadLinkMatcherTest
{
  [Fact]
  public void Match_ByFileName_ReturnsMatchingInfo()
  {
    var download = new Download
    {
      FileName = "episode.mkv",
      Path = "https://cdn.example.com/old-token/episode.mkv"
    };

    var infos = new List<DownloadInfo>
    {
      new() { FileName = "other.mkv", RestrictedLink = "https://cdn.example.com/new/other.mkv" },
      new() { FileName = "episode.mkv", RestrictedLink = "https://cdn.example.com/new/episode.mkv" }
    };

    var result = DownloadLinkMatcher.Match(infos, download);

    Assert.NotNull(result);
    Assert.Equal("https://cdn.example.com/new/episode.mkv", result.RestrictedLink);
  }

  [Fact]
  public void Match_ByUrlSegment_WhenFileNameMissing_ReturnsMatchingInfo()
  {
    var download = new Download
    {
      Path = "https://cdn.example.com/old-token/Yakuza.Fianc%C3%A9.S01E05.mkv"
    };

    var infos = new List<DownloadInfo>
    {
      new() { FileName = "Yakuza.Fiancé.S01E05.mkv", RestrictedLink = "https://cdn.example.com/new-token/Yakuza.Fianc%C3%A9.S01E05.mkv" }
    };

    var result = DownloadLinkMatcher.Match(infos, download);

    Assert.NotNull(result);
    Assert.Equal("https://cdn.example.com/new-token/Yakuza.Fianc%C3%A9.S01E05.mkv", result.RestrictedLink);
  }

  [Fact]
  public void Match_SingleFileTorrent_ReturnsOnlyInfo()
  {
    var download = new Download
    {
      Path = "https://cdn.example.com/old-token/movie.mkv"
    };

    var infos = new List<DownloadInfo>
    {
      new() { FileName = "movie.mkv", RestrictedLink = "https://cdn.example.com/new-token/movie.mkv" }
    };

    var result = DownloadLinkMatcher.Match(infos, download);

    Assert.NotNull(result);
    Assert.Equal("https://cdn.example.com/new-token/movie.mkv", result.RestrictedLink);
  }

  [Fact]
  public void Match_MultipleFilesWithoutMatch_ReturnsNull()
  {
    var download = new Download
    {
      Path = "https://cdn.example.com/old-token/unknown.mkv"
    };

    var infos = new List<DownloadInfo>
    {
      new() { FileName = "a.mkv", RestrictedLink = "https://cdn.example.com/new/a.mkv" },
      new() { FileName = "b.mkv", RestrictedLink = "https://cdn.example.com/new/b.mkv" }
    };

    var result = DownloadLinkMatcher.Match(infos, download);

    Assert.Null(result);
  }
}
