using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using SharpDX;

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

    public static T Clamp<T>(T value, T min, T max) where T : IComparable<T> 
    => value.CompareTo(min) < 0 ? min : value.CompareTo(max) > 0 ? max : value;


    public static Transform3D MakeCorrectRotationMatrix(Vector3 euler, Vector3 centerPoint)
    {
        // Convert angles to radians
        double pitch = euler.Y * Math.PI / -180; // Y rotation (pitch)
        double yaw = euler.X * Math.PI / 180;   // X rotation (yaw)
        double roll = euler.Z * Math.PI / 180;  // Z rotation (roll)

        // Pre-calculate trigonometric values
        double cp = Math.Cos(pitch);
        double sp = Math.Sin(pitch);
        double cy = Math.Cos(yaw);
        double sy = Math.Sin(yaw);
        double cr = Math.Cos(roll);
        double sr = Math.Sin(roll);

        // Create the rotation matrix
        Matrix3D rotationMatrix = new Matrix3D(
            cp * cr,                   -cp * sr,                sp,                 0,  // Row 1
            cy * sr + sy * sp * cr,    cy * cr - sy * sp * sr, -sy * cp,          0,  // Row 2
            sy * sr - cy * sp * cr,    sy * cr + cy * sp * sr,  cy * cp,          0,  // Row 3
            0,                         0,                       0,                  1   // Row 4
        );

        // Create a transform group to combine transformations
        Transform3DGroup transformGroup = new Transform3DGroup();
    
        // Add translation to origin
        transformGroup.Children.Add(new TranslateTransform3D(
            -centerPoint.X, 
            -centerPoint.Y, 
            -centerPoint.Z
        ));
    
        // Add rotation
        transformGroup.Children.Add(new MatrixTransform3D(rotationMatrix));
    
        // Add translation back
        transformGroup.Children.Add(new TranslateTransform3D(
            centerPoint.X,
            centerPoint.Y,
            centerPoint.Z
        ));

        return transformGroup;
    }
}