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
}