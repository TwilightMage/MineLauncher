using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using MineLauncher.Commands;

namespace MineLauncher.Screens;

public partial class LogsScreen : UserControl
{
    private Command _openLatestLogCommand;
    public Command OpenLatestLogCommand => _openLatestLogCommand ??= new RelayCommand(() =>
    {
        System.Diagnostics.Process.Start(App.Instance.LatestLogFile);
    }, () => File.Exists(App.Instance.LatestLogFile));
    
    private Command _copyLogCommand;
    public Command CopyLogCommand => _copyLogCommand ??= new RelayCommand(() =>
    {
        if (ListViewWidget.SelectedItems.Count != 0)
        {
            Clipboard.SetText(string.Join(Environment.NewLine, ListViewWidget.SelectedItems.Cast<string>()));
        }
    });
    
    private ScrollViewer _scrollViewer;
    private bool _atBottom;
    
    public LogsScreen()
    {
        InitializeComponent();
        
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _scrollViewer = (ScrollViewer)typeof(ItemsControl).GetProperty("ScrollHost", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(ListViewWidget);
        _scrollViewer.ScrollChanged += (s, args) =>
        {
            _atBottom = _scrollViewer.VerticalOffset == _scrollViewer.ScrollableHeight;
        };
        
        App.Instance.LogWatcher.CollectionChanged += (s, args) =>
        {
            if (_atBottom)
                _scrollViewer.ScrollToBottom();
        };
    }
}