using System;

namespace MineLauncher;

public static class Extensions
{
    public static T Apply<T>(this T item, Action<T> action)
    {
        action(item);
        return item;
    }
    
    public static string FormatInline(this string format, params object[] args) => string.Format(format, args);
}