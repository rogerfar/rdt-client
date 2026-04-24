using System.Text;
using System.Text.RegularExpressions;
using RdtClient.Service.Services;

namespace RdtClient.Service.Helpers;

/// <summary>
/// Works around a known Linux-only failure in the Bezzad Downloader NuGet
/// package, which throws "Access to the path is denied" when the target
/// path contains square brackets, curly braces, or runs of consecutive
/// whitespace — all common in debrid-provider release names.
///
/// Path.GetInvalidFileNameChars() on Linux only returns NUL and '/', so
/// these characters pass through .NET's built-in filter unchanged.
/// Control characters are also stripped as a defensive measure.
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
    /// When sanitization is enabled, returns <see cref="SanitizeFilename"/>.
    /// When disabled, returns the input as-is; null is coerced to empty string
    /// so callers can treat the return as non-null.
    /// </summary>
    public static String SanitizeFilenameIfEnabled(String? filename)
    {
        return IsEnabled ? SanitizeFilename(filename) : filename ?? String.Empty;
    }

    /// <summary>
    /// When sanitization is enabled, returns <see cref="SanitizePath"/>.
    /// When disabled, returns the input as-is; null is coerced to empty string
    /// so callers can treat the return as non-null.
    /// </summary>
    public static String SanitizePathIfEnabled(String? filePath)
    {
        return IsEnabled ? SanitizePath(filePath) : filePath ?? String.Empty;
    }

    /// <summary>
    /// Strips square brackets, curly braces, and control characters, collapses
    /// runs of whitespace to a single space, and trims. Operates on a single
    /// filename segment; does not interpret directory separators.
    /// </summary>
    public static String SanitizeFilename(String? filename)
    {
        if (String.IsNullOrEmpty(filename))
        {
            return String.Empty;
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
    /// Splits on both '/' and '\' regardless of host OS so paths that arrive
    /// from the debrid provider with foreign separators are still handled;
    /// the result is re-joined with the host's native separator.
    /// </summary>
    public static String SanitizePath(String? filePath)
    {
        if (String.IsNullOrEmpty(filePath))
        {
            return String.Empty;
        }

        var segments = filePath.Split('/', '\\');

        for (var i = 0; i < segments.Length; i++)
        {
            if (!String.IsNullOrEmpty(segments[i]))
            {
                segments[i] = SanitizeFilename(segments[i]);
            }
        }

        return String.Join(Path.DirectorySeparatorChar, segments);
    }
}
