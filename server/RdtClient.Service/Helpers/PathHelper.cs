namespace RdtClient.Service.Helpers;

/// <summary>
///     Categories are supplied by external clients (the qBittorrent and SabNZBd APIs, the web UI) and are appended to the
///     download path. Without sanitizing them a category such as "/etc" or "../../.." escapes the download root, because
///     <see cref="Path.Combine(String, String)" /> discards the first argument entirely when the second one is rooted.
/// </summary>
public static class PathHelper
{
    private static readonly Char[] InvalidSegmentChars = Path.GetInvalidFileNameChars()
                                                             .Concat(['/', '\\', ':'])
                                                             .Distinct()
                                                             .ToArray();

    private static readonly StringComparison PathComparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    /// <summary>
    ///     Reduces a category to a relative, traversal-free path. Nested categories ("tv/sonarr") are preserved because they
    ///     are a supported use case; only the segments that could escape the root are dropped.
    /// </summary>
    /// <returns>A relative path, or <c>null</c> when nothing usable remains.</returns>
    public static String? SanitizeCategory(String? category)
    {
        if (String.IsNullOrWhiteSpace(category))
        {
            return null;
        }

        var segments = category.Replace('\\', '/')
                               .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var safeSegments = new List<String>();

        foreach (var segment in segments)
        {
            // Stripping the separators guarantees a segment can never re-introduce nesting, and trimming the dots turns
            // "." and ".." into an empty string, which drops the segment entirely.
            var cleaned = String.Concat(segment.Split(InvalidSegmentChars))
                                .Trim()
                                .Trim('.')
                                .Trim();

            if (cleaned.Length == 0)
            {
                continue;
            }

            safeSegments.Add(cleaned);
        }

        return safeSegments.Count == 0 ? null : String.Join(Path.DirectorySeparatorChar, safeSegments);
    }

    /// <summary>
    ///     Appends a sanitized category to <paramref name="root" />, guaranteeing the result stays inside the root.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">The resolved path escaped the root.</exception>
    public static String CombineWithCategory(String root, String? category)
    {
        var sanitized = SanitizeCategory(category);

        if (sanitized == null)
        {
            return root;
        }

        var combined = Path.Combine(root, sanitized);

        if (!IsWithin(root, combined))
        {
            throw new UnauthorizedAccessException($"Category '{category}' resolves outside of the download path '{root}'");
        }

        return combined;
    }

    /// <summary>
    ///     Checks that <paramref name="candidate" /> resolves to <paramref name="root" /> or one of its descendants.
    /// </summary>
    public static Boolean IsWithin(String root, String candidate)
    {
        if (String.IsNullOrWhiteSpace(root) || String.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        String rootFull;
        String candidateFull;

        try
        {
            rootFull = Path.GetFullPath(root);
            candidateFull = Path.GetFullPath(candidate);
        }
        catch (Exception)
        {
            // An unusable path is never considered contained.
            return false;
        }

        rootFull = rootFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        candidateFull = candidateFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (String.Equals(rootFull, candidateFull, PathComparison))
        {
            return true;
        }

        return candidateFull.StartsWith(rootFull + Path.DirectorySeparatorChar, PathComparison);
    }
}
