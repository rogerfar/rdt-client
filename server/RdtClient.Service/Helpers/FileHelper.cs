namespace RdtClient.Service.Helpers;

public static class FileHelper
{
    public static async Task Delete(String path)
    {
        if (String.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (!File.Exists(path))
        {
            return;
        }

        var retry = 0;

        while (true)
        {
            try
            {
                File.Delete(path);

                break;
            }
            catch
            {
                if (retry >= 3)
                {
                    throw;
                }

                retry++;

                await Task.Delay(1000 * retry);
            }
        }
    }

    public static async Task DeleteDirectory(String path)
    {
        if (String.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (!Directory.Exists(path))
        {
            return;
        }

        var retry = 0;

        while (true)
        {
            try
            {
                Directory.Delete(path, true);

                break;
            }
            catch
            {
                if (retry >= 3)
                {
                    throw;
                }

                retry++;

                await Task.Delay(1000 * retry);
            }
        }
    }

    public static String RemoveInvalidFileNameChars(String filename)
    {
        return String.Concat(filename.Split(Path.GetInvalidFileNameChars()));
    }
}