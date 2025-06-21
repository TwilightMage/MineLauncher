using System;
using System.IO;
using System.Text.RegularExpressions;

namespace MineLauncher;

public static class Extensions
{
    public static T Apply<T>(this T item, Action<T> action)
    {
        action(item);
        return item;
    }
    
    public static string ToValidName(this string name) => Regex.Replace(name, @"[^a-zA-Z0-9_]+", "_");
    
    public static string FormatInline(this string format, params object[] args) => string.Format(format, args);
    
    public static string ReadLineAdvanced(this Stream stream, out long moved, out long lineStart, out long lineLength)
    {
        moved = 0;
        long start = stream.Position;

        lineStart = start;
        lineLength = 0;
        bool lineStarted = false, lineFinished = false;
        
        while (stream.Position < stream.Length)
        {
            int b = stream.ReadByte();
            if (b == -1)
            {
                if (lineFinished)
                    break;
                
                stream.Position = start;
                moved = 0;
                return null;
            }
            
            bool isLineEnd = (char)b == '\n';
            bool isTrimChar = (char)b == '\n' || (char)b == '\r';
            
            moved++;

            if (!isTrimChar)
            {
                if (!lineStarted)
                {
                    lineStart = stream.Position - 1;
                    lineStarted = true;
                }
                else if (lineFinished)
                {
                    moved--;
                    break;
                }
            }
            else
            {
                if (!isLineEnd)
                {
                    lineLength = stream.Position - lineStart - 1;
                    lineFinished = true;
                }
            }
        }

        if (stream.Position == stream.Length)
        {
            moved = 0;
            return null;
        }

        if (lineLength > 0)
        {
            stream.Position = lineStart;
            byte[] buffer = new byte[lineLength];
            stream.Read(buffer, 0, (int)lineLength);
            stream.Position = start + moved;
            return System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        }
        
        stream.Position = start + moved;
        return "";
    }
}