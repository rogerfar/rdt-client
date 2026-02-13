using RdtClient.Service.Helpers;

namespace RdtClient.Service.Test.Helpers;

public class FileHelperTest
{
    [Fact]
    public void RemoveInvalidFileNameChars_RemovesInvalidChars()
    {
        // Arrange
        var invalidChars = Path.GetInvalidFileNameChars();
        var input = "test" + new String(invalidChars) + "file.txt";

        // Act
        var result = FileHelper.RemoveInvalidFileNameChars(input);

        // Assert
        Assert.Equal("testfile.txt", result);
    }

    [Fact]
    public void RemoveInvalidFileNameChars_RemovesDirectorySeparators()
    {
        // Arrange
        var input = "folder/subfolder\\file.txt";

        // Act
        var result = FileHelper.RemoveInvalidFileNameChars(input);

        // Assert
        Assert.Equal("foldersubfolderfile.txt", result);
    }

    [Fact]
    public void RemoveInvalidFileNameChars_RemovesDoubleDots()
    {
        // Arrange
        var input = "test..file.txt";

        // Act
        var result = FileHelper.RemoveInvalidFileNameChars(input);

        // Assert
        Assert.Equal("testfile.txt", result);
    }

    [Fact]
    public void RemoveInvalidFileNameChars_TrimsLeadingSeparators()
    {
        // Arrange
        var input = "/test/file.txt";

        // Act
        var result = FileHelper.RemoveInvalidFileNameChars(input);

        // Assert
        // Note: The method first splits by invalid chars (including /), 
        // so "/test/file.txt" becomes "testfile.txt" through String.Concat(Split).
        // Then it trims leading separators.
        Assert.Equal("testfile.txt", result);
    }

    [Fact]
    public void RemoveInvalidFileNameChars_HandlesValidFileName()
    {
        // Arrange
        var input = "valid_file-123.txt";

        // Act
        var result = FileHelper.RemoveInvalidFileNameChars(input);

        // Assert
        Assert.Equal("valid_file-123.txt", result);
    }

    [Fact]
    public void RemoveInvalidFileNameChars_HandlesEmptyString()
    {
        // Arrange
        var input = "";

        // Act
        var result = FileHelper.RemoveInvalidFileNameChars(input);

        // Assert
        Assert.Equal("", result);
    }

    [Theory]
    [InlineData("test/../file.txt", "testfile.txt")]
    [InlineData(".../test.txt", ".test.txt")]
    [InlineData("test....txt", "testtxt")]
    public void RemoveInvalidFileNameChars_ComplexCases(String input, String expected)
    {
        // Act
        var result = FileHelper.RemoveInvalidFileNameChars(input);

        // Assert
        Assert.Equal(expected, result);
    }
}
