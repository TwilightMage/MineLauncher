using System;
using System.IO;
using System.Threading;

namespace MineLauncher;

public class LogFileWatcher : IDisposable
{
    public event Action<string> LinePrint;
    
    private readonly string _filePath;
    private readonly Timer _pollTimer;
    private long _lastPosition;
    private bool _disposed;
    private DateTime _lastReadTime = DateTime.MinValue;
    private readonly object _lock = new object();

    public LogFileWatcher(string filePath, int pollInterval = 200)
    {
        _filePath = filePath;
        _lastPosition = 0;
        
        // Initialize timer
        _pollTimer = new Timer(CheckFile, null, pollInterval, pollInterval);
        
        // Set initial position
        UpdateFilePosition();
    }

    private void UpdateFilePosition()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                using (var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    _lastPosition = fs.Length;
                }
            }
        }
        catch
        {
            // Ignore errors during initialization
        }
    }

    private void CheckFile(object state)
    {
        // Prevent overlapping checks
        if (Monitor.TryEnter(_lock))
        {
            try
            {
                // Check if file exists
                if (!File.Exists(_filePath)) return;

                // Get current file info
                var currentLastWrite = File.GetLastWriteTimeUtc(_filePath);
                
                // Skip if file hasn't changed since last read
                if (currentLastWrite <= _lastReadTime) return;
                
                // Read new lines
                ReadNewLines();
                
                // Update last read time
                _lastReadTime = DateTime.UtcNow;
            }
            catch
            {
                // Ignore errors during polling
            }
            finally
            {
                Monitor.Exit(_lock);
            }
        }
    }

    private void ReadNewLines()
    {
        try
        {
            using (var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                // Handle file truncation or rotation
                if (fs.Length < _lastPosition)
                {
                    _lastPosition = 0;
                }

                // Skip if no new content
                if (fs.Length <= _lastPosition) return;

                // Read new content
                fs.Seek(_lastPosition, SeekOrigin.Begin);
                using (var reader = new StreamReader(fs))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (!string.IsNullOrEmpty(line))
                        {
                            LinePrint?.Invoke(line);
                        }
                    }
                    _lastPosition = fs.Position;
                }
            }
        }
        catch
        {
            // Ignore read errors (will try again next poll)
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _pollTimer?.Dispose();
        _disposed = true;
    }
}