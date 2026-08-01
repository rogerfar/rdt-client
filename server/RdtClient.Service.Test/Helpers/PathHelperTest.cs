using RdtClient.Service.Helpers;

namespace RdtClient.Service.Test.Helpers;

public class PathHelperTest
{
    private static readonly String Root = Path.Combine(Path.GetTempPath(), "rdt-download-root");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("..")]
    [InlineData("../..")]
    [InlineData(".")]
    [InlineData("/")]
    [InlineData("///")]
    public void SanitizeCategory_ReturnsNull_ForEmptyOrTraversalOnlyInput(String? category)
    {
        Assert.Null(PathHelper.SanitizeCategory(category));
    }

    [Theory]
    [InlineData("movies", "movies")]
    [InlineData("tv-sonarr", "tv-sonarr")]
    [InlineData("  movies  ", "movies")]
    public void SanitizeCategory_KeepsSimpleCategories(String category, String expected)
    {
        Assert.Equal(expected, PathHelper.SanitizeCategory(category));
    }

    [Fact]
    public void SanitizeCategory_PreservesNesting()
    {
        // Nested categories are a supported use case and must keep working.
        Assert.Equal(Path.Combine("tv", "sonarr"), PathHelper.SanitizeCategory("tv/sonarr"));
        Assert.Equal(Path.Combine("tv", "sonarr"), PathHelper.SanitizeCategory("tv\\sonarr"));
        Assert.Equal(Path.Combine("tv", "sonarr"), PathHelper.SanitizeCategory("/tv//sonarr/"));
    }

    [Theory]
    [InlineData("/etc", "etc")]
    [InlineData("../../etc", "etc")]
    [InlineData("..\\..\\windows", "windows")]
    [InlineData("movies/../../../etc", "movies|etc")]
    public void SanitizeCategory_StripsTraversalSegments(String category, String expectedSegments)
    {
        var expected = String.Join(Path.DirectorySeparatorChar, expectedSegments.Split('|'));

        Assert.Equal(expected, PathHelper.SanitizeCategory(category));
    }

    [Fact]
    public void SanitizeCategory_StripsDriveQualifier()
    {
        // "C:\Windows" must not stay rooted, otherwise Path.Combine would discard the download path.
        var result = PathHelper.SanitizeCategory("C:\\Windows");

        Assert.NotNull(result);
        Assert.False(Path.IsPathRooted(result));
    }

    [Fact]
    public void CombineWithCategory_ReturnsRoot_WhenCategoryIsEmpty()
    {
        Assert.Equal(Root, PathHelper.CombineWithCategory(Root, null));
        Assert.Equal(Root, PathHelper.CombineWithCategory(Root, "  "));
        Assert.Equal(Root, PathHelper.CombineWithCategory(Root, ".."));
    }

    [Fact]
    public void CombineWithCategory_AppendsCategoryUnderRoot()
    {
        Assert.Equal(Path.Combine(Root, "movies"), PathHelper.CombineWithCategory(Root, "movies"));
        Assert.Equal(Path.Combine(Root, "tv", "sonarr"), PathHelper.CombineWithCategory(Root, "tv/sonarr"));
    }

    [Theory]
    [InlineData("/etc")]
    [InlineData("../../../etc")]
    [InlineData("..\\..\\..\\windows\\system32")]
    [InlineData("C:\\Windows")]
    [InlineData("movies/../../../../root")]
    public void CombineWithCategory_NeverEscapesTheRoot(String category)
    {
        // This is the regression guard: an absolute or traversing category used to escape the download path, which for
        // the deletion code paths meant recursively deleting a directory outside of it.
        var result = PathHelper.CombineWithCategory(Root, category);

        Assert.True(PathHelper.IsWithin(Root, result), $"'{category}' resolved to '{result}', outside of '{Root}'");
    }

    [Fact]
    public void IsWithin_AcceptsRootItselfAndDescendants()
    {
        Assert.True(PathHelper.IsWithin(Root, Root));
        Assert.True(PathHelper.IsWithin(Root, Root + Path.DirectorySeparatorChar));
        Assert.True(PathHelper.IsWithin(Root, Path.Combine(Root, "movies", "a.mkv")));
    }

    [Fact]
    public void IsWithin_RejectsOutsidePaths()
    {
        Assert.False(PathHelper.IsWithin(Root, Path.Combine(Root, "..", "other")));
        Assert.False(PathHelper.IsWithin(Root, Root + "-sibling"));
        Assert.False(PathHelper.IsWithin(Root, ""));
        Assert.False(PathHelper.IsWithin("", Root));
    }
}
