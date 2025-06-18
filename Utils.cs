using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

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
    
    public static void UpdateAllLocalizationBindings(DependencyObject parent)
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);

        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is FrameworkElement element)
            {
                BindingOperations.GetBindingExpression(element, FrameworkElement.LanguageProperty)?.UpdateTarget();

                element.Resources.MergedDictionaries.Clear();
            }

            UpdateAllLocalizationBindings(child);
        }
    }
    public static bool TryParseDateTime(string input, out DateTime result)
    {
        // Common formats list (modify/extend as needed)
        var formats = new[]
        {
            // ISO 8601 variants
            "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
            "yyyy-MM-dd HH:mm:ss.FFFFFFF",
            "yyyyMMddTHHmmssfff",
            
            // With comma as millisecond separator
            "yyyy-MM-dd HH:mm:ss,fff",
            "yyyy-MM-ddTHH:mm:ss,fff",
            
            // Without milliseconds
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            
            // Date-only formats
            "yyyy-MM-dd",
            "MM/dd/yyyy",
            "dd/MM/yyyy"
        };

        return DateTime.TryParseExact(
            input,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out result
        );
    }
}