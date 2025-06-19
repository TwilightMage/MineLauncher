using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Material.Icons;

namespace MineLauncher;

public partial class LogEntry : UserControl, INotifyPropertyChanged
{
    public static readonly DependencyProperty LogLineProperty = 
        DependencyProperty.Register(nameof(LogLine), typeof(string), typeof(LogEntry), new PropertyMetadata((o,
            args) =>
        {
            var logEntry = (LogEntry)o;
            
            var match = Regex.Match(args.NewValue.ToString(), @"^\[(?<time>.*?)\]\s*\[(?<thread>.*?)\/(?<level>\w+)\]:\s*(?<message>.*)");
            if (match.Success)
            {
                switch (match.Groups["level"].Value)
                {
                    case "TRACE":
                        logEntry.IconKind = MaterialIconKind.Code;
                        logEntry.IconBrush = Brushes.Gray;
                        break;
                    case "DEBUG":
                        logEntry.IconKind = MaterialIconKind.Info;
                        logEntry.IconBrush = Brushes.RoyalBlue;
                        break;
                    case "INFO":
                        logEntry.IconKind = MaterialIconKind.Info;
                        logEntry.IconBrush = Brushes.Green;
                        break;
                    case "WARN":
                        logEntry.IconKind = MaterialIconKind.Warning;
                        logEntry.IconBrush = Brushes.Orange;
                        break;
                    case "ERROR":
                        logEntry.IconKind = MaterialIconKind.Error;
                        logEntry.IconBrush = Brushes.Red;
                        break;
                    case "FATAL":
                        logEntry.IconKind = MaterialIconKind.Error;
                        logEntry.IconBrush = Brushes.Red;
                        break;
                    default:
                        logEntry.IconKind = MaterialIconKind.Info;
                        logEntry.IconBrush = Brushes.Transparent;
                        break;
                }
            }
            else
            {
                logEntry.IconKind = MaterialIconKind.Info;
                logEntry.IconBrush = Brushes.Transparent;
            }
            
            logEntry.OnPropertyChanged(nameof(IconKind));
            logEntry.OnPropertyChanged(nameof(IconBrush));
        }));
    
    public string LogLine
    {
        get => (string)GetValue(LogLineProperty);
        set => SetValue(LogLineProperty, value);
    }
    
    public MaterialIconKind IconKind { get; private set; }
    public Brush IconBrush { get; private set; }
    
    public LogEntry()
    {
        InitializeComponent();
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
}