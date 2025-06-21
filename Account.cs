using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using MineLauncher.Commands;

namespace MineLauncher;

public class Account : INotifyPropertyChanged
{
    public string Username { get; set; }
    public string Token { get; set; }
    
    private bool _slimModel;
    public bool SlimModel
    {
        get => _slimModel;
        set => SetField(ref _slimModel, value);
    }
    
    private Image _playerSkin;
    public Image PlayerSkin
    {
        get => _playerSkin;
        private set => SetField(ref _playerSkin, value);
    }
    public void SetPlayerSkin(string url) => PlayerSkin = string.IsNullOrEmpty(url)
        ? null
        : Image.FromFile(url);

    private Image _capeSkin;
    public Image CapeSkin
    {
        get => _capeSkin;
        private set => SetField(ref _capeSkin, value);
    }
    public void SetCapeSkin(string url) => CapeSkin = string.IsNullOrEmpty(url)
        ? null
        : Image.FromFile(url);
    
    private Command _browsePlayerSkinCommand;
    public Command BrowsePlayerSkinCommand => _browsePlayerSkinCommand ??= new RelayCommand(() =>
    {
        OpenFileDialog dialog = new();
            
        var result = dialog.ShowDialog();  
        if (result == DialogResult.OK && Image.FromFile(dialog.FileName) is { } image)  
        {  
            PlayerSkin = image;  
        }
    });
    
    private Command _browseCapeSkinCommand;
    public Command BrowseCapeSkinCommand => _browseCapeSkinCommand ??= new RelayCommand(() =>
    {
        OpenFileDialog dialog = new();
            
        var result = dialog.ShowDialog();  
        if (result == DialogResult.OK && Image.FromFile(dialog.FileName) is { } image)  
        {  
            CapeSkin = image;  
        }
    });

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