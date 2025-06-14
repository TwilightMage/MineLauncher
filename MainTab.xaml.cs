using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace MineLauncher;

public partial class MainTab : RadioButton
{
    public static event Action<string, string> GroupItemSelected;

    private static readonly Dictionary<string, Dictionary<string, MainTab>> GroupItemMap = new();
    private static readonly Dictionary<string, string> GroupItemSelections = new();
    
    public static readonly DependencyProperty ScreenProperty = 
        DependencyProperty.Register(nameof(Screen), typeof(object), typeof(MainTab), new PropertyMetadata(null));
    
    public static readonly DependencyProperty GroupItemNameProperty = 
        DependencyProperty.Register(nameof(GroupItemName), typeof(string), typeof(MainTab), new PropertyMetadata((o,
            args) =>
        {
            var tab = (MainTab)o;
            RemoveGroupItem(tab.GroupName, (string)args.OldValue);
            AddGroupItem(tab.GroupName, (string)args.NewValue, tab);

            var groupSelection = GetGroupSelection(tab.GroupName);

            if (groupSelection == (string)args.OldValue) // moving from the selected item name
                tab.IsChecked = false;
            else if (groupSelection == (string)args.NewValue) // moving to the selected item name
                tab.IsChecked = true;
        }));
    
    public object Screen
    {
        get => GetValue(ScreenProperty);
        set => SetValue(ScreenProperty, value);
    }
    
    public string GroupItemName
    {
        get => (string)GetValue(GroupItemNameProperty);
        set => SetValue(GroupItemNameProperty, value);
    }

    private static void RemoveGroupItem(string groupName, string itemName)
    {
        if (groupName == null || itemName == null)
            return;
        
        if (GroupItemMap.TryGetValue(groupName, out var itemMap))
        {
            itemMap.Remove(itemName);

            if (itemMap.Count == 0)
                GroupItemMap.Remove(groupName);
        }
    }
    
    private static void AddGroupItem(string groupName, string itemName, MainTab tab)
    {
        if (groupName == null || itemName == null || tab == null)
            return;
        
        if (GroupItemMap.TryGetValue(groupName, out var itemMap))
            itemMap.Add(itemName, tab);
        else
            GroupItemMap.Add(groupName, new Dictionary<string, MainTab>()
            {
                [itemName] = tab
            });
    }

    public static MainTab GetTabByItemName(string groupName, string itemName)
    {
        if (groupName == null || itemName == null)
            return null;
        
        if (GroupItemMap.TryGetValue(groupName, out var itemMap))
            if (itemMap.TryGetValue(itemName, out var tab))
                return tab;

        return null;
    }
    
    public static string GetGroupSelection(string groupName)
    {
        if (groupName == null)
            return null;
        
        if (GroupItemSelections.TryGetValue(groupName, out var selection))
            return selection;

        return null;
    }

    public static MainTab GetSelectedTabInGroup(string groupName) => GetTabByItemName(groupName, GetGroupSelection(groupName));

    public static void SelectTabByItemName(string groupName, string itemName)
    {
        var currentSelection = GetGroupSelection(groupName);
        
        if (currentSelection == itemName)
            return;

        if (GetTabByItemName(groupName, itemName) is { } existingTab)
            existingTab.IsChecked = true;
        else
        {
            GroupItemSelections[groupName] = itemName;
            GroupItemSelected?.Invoke(groupName, itemName);
        }
    }
    
    public MainTab()
    {
        InitializeComponent();
        GroupName = "MainTabs";

        Checked += MainTab_Checked;
    }
    
    private void MainTab_Checked(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.SelectedScreen = Screen;
        }
        
        GroupItemSelections[GroupName] = GroupItemName;
        GroupItemSelected?.Invoke(GroupName, GroupItemName);
    }
}