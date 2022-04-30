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
}