using System;
using System.Windows;
using System.Windows.Controls;

namespace MineLauncher;

public partial class MainTabScreen : UserControl
{
    public static readonly DependencyProperty ScreenContentProperty =
        DependencyProperty.Register(nameof(ScreenContent), typeof(object), typeof(MainTabScreen), 
            new PropertyMetadata(null));

    public object ScreenContent
    {
        get => GetValue(ScreenContentProperty);
        set => SetValue(ScreenContentProperty, value);
    }
    
    public MainTabScreen()
    {
        InitializeComponent();
    }
}