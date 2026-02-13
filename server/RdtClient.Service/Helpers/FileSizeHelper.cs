namespace RdtClient.Service.Helpers;

public static class FileSizeHelper
{
    public static String FormatSize(Int64? bytes)
    {
        if (bytes == null)
        {
            return "0 B";
        }

        String[] units = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
        var unitIndex = 0;
        Double size = bytes.Value;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.##} {units[unitIndex]}";
    }
}
