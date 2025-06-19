using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace MineLauncher;

public class LogFileWatcher : IDisposable, INotifyPropertyChanged, INotifyCollectionChanged, IList<string>
{
    private class VirtualizedEnumerator(LogFileWatcher source) : IEnumerator<string>
    {
        private int _index = -1;

        public string Current => source[_index];
        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            _index++;
            return _index < source.Count;
        }

        public void Reset() => _index = -1;
        public void Dispose() { }
    }
    
    public event Action Reset;
    public event Action<string> LinePrint;
    
    private readonly string _filePath;
    private readonly Timer _pollTimer;
    private long _lastPosition;
    private bool _disposed;
    private DateTime _lastReadTime = DateTime.UtcNow;
    private readonly object _lock = new object();

    private readonly List<KeyValuePair<long, ushort>> _linePositions = new();
    
    private bool _resetting;
    private List<string> _newLines = new();

    public LogFileWatcher(string filePath, int pollInterval = 200)
    {
        _filePath = filePath;
        _lastPosition = 0;
        
        // Initialize timer
        _pollTimer = new Timer(CheckFile, null, pollInterval, pollInterval);
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

                if (_resetting || _newLines.Count > 0)
                {
                    App.Instance.Dispatcher.Invoke(() =>
                    {
                        if (_resetting)
                        {
                            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                            _resetting = false;
                        }
                        
                        if (_newLines.Count > 0)
                        {
                            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, _newLines));
                            _newLines.Clear();
                        }
                    });
                }
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
                    _linePositions.Clear();
                    OnPropertyChanged(nameof(Count));
                    Reset?.Invoke();
                    _resetting = true;
                }

                // Skip if no new content
                if (fs.Length <= _lastPosition) return;

                // Read new content
                fs.Seek(_lastPosition, SeekOrigin.Begin);
                while (fs.ReadLineAdvanced(out long _, out long lineStart, out long lineLength) is { } line)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        _linePositions.Add(new (lineStart, (ushort)lineLength));
                        OnPropertyChanged(nameof(Count));
                        LinePrint?.Invoke(line);
                        _newLines.Add(line);
                    }
                }
                _lastPosition = fs.Position;
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

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public IEnumerator<string> GetEnumerator() => new VirtualizedEnumerator(this);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(string item) => throw new NotImplementedException();
    public void Clear() => throw new NotImplementedException();
    public bool Contains(string item) => throw new NotImplementedException();
    public void CopyTo(string[] array, int arrayIndex) => throw new NotImplementedException();
    public bool Remove(string item) => throw new NotImplementedException();

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _linePositions.Count;
            }
        }
    }
    
    public bool IsReadOnly => true;
    
    public int IndexOf(string item) => throw new NotImplementedException();
    public void Insert(int index, string item) => throw new NotImplementedException();
    public void RemoveAt(int index) => throw new NotImplementedException();

    public string this[int index]
    {
        get
        {
            lock (_lock)
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException();

                var lineInfo = _linePositions[index];

                using (var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    fs.Seek(lineInfo.Key, SeekOrigin.Begin);
                    byte[] buffer = new byte[lineInfo.Value];
                    fs.Read(buffer, 0, lineInfo.Value);
                    return Encoding.UTF8.GetString(buffer).TrimEnd('\r', '\n');
                }
            }
        }
        set => throw new NotSupportedException();
    }

    public event NotifyCollectionChangedEventHandler CollectionChanged;
}