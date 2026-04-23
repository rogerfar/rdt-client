using System.Text;
using System.Text.RegularExpressions;
using RdtClient.Service.Services;

namespace RdtClient.Service.Helpers;

/// <summary>
/// Sanitizes filenames to prevent issues with special characters on Linux containers.
/// Path.GetInvalidFileNameChars() on Linux only returns NUL and '/', so characters
/// like [ ] { } that cause problems with shell globbing, URI handling, and various
/// download clients pass through the built-in filter unchanged.
/// </summary>
public static partial class FilenameSanitizer
{
    [GeneratedRegex(@" {2,}")]
    private static partial Regex CreateMultipleSpacesRegex();

    private static readonly Regex MultipleSpaces = CreateMultipleSpacesRegex();

    /// <summary>
    /// Returns whether sanitization is enabled in the current settings.
    /// </summary>
    public static Boolean IsEnabled => Settings.Get.DownloadClient.SanitizeFilenames;

    /// <summary>
    /// Sanitizes a filename if enabled in settings; otherwise returns it unchanged.
    /// </summary>
    public static String SanitizeFilenameIfEnabled(String filename)
    {
        return IsEnabled ? SanitizeFilename(filename) : filename;
    }

    /// <summary>
    /// Sanitizes a full path if enabled in settings; otherwise returns it unchanged.
    /// </summary>
    public static String SanitizePathIfEnabled(String filePath)
    {
        return IsEnabled ? SanitizePath(filePath) : filePath;
    }

    /// <summary>
    /// Sanitizes a filename by stripping problematic characters and normalizing whitespace.
    /// Does NOT touch directory separators — only a single filename segment.
    /// </summary>
    public static String SanitizeFilename(String filename)
    {
        if (String.IsNullOrEmpty(filename))
        {
            return filename;
        }

        var sb = new StringBuilder(filename.Length);

        foreach (var c in filename)
        {
            if (Char.IsControl(c))
            {
                continue;
            }

            if (c is '[' or ']' or '{' or '}')
            {
                continue;
            }

            sb.Append(c);
        }

        var result = sb.ToString();
        result = MultipleSpaces.Replace(result, " ");
        result = result.Trim();

        return result;
    }

    /// <summary>
    /// Sanitizes each segment of a full file path, preserving directory separators.
    /// </summary>
    public static String SanitizePath(String filePath)
    {
        if (String.IsNullOrEmpty(filePath))
        {
            return filePath;
        }

        var separator = Path.DirectorySeparatorChar;
        var segments = filePath.Split(separator);

        for (var i = 0; i < segments.Length; i++)
        {
            if (!String.IsNullOrEmpty(segments[i]))
            {
                segments[i] = SanitizeFilename(segments[i]);
            }
        }

        return String.Join(separator, segments);
    }
}
