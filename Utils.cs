namespace MineLauncher;

public static class Utils
{
    public static string FormatSize(ulong bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
        int order = 0;
        double size = bytes;
    
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
    
        return $"{size:0.##} {sizes[order]}";
    }
}