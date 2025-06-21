using System.Drawing;

namespace MineLauncher;

public class Account
{
    public string Username { get; set; }
    public string Token { get; set; }
    public bool SlimModel { get; set; } = false;
    public Image PlayerSkin { get; set; } = Image.FromFile(@"C:\Users\Dragon\Downloads\c84fc2525217e7ad.png");
    public Image CapeSkin { get; set; } = Image.FromFile(@"C:\Users\Dragon\Downloads\cape_microsoft_migration_5b37a01fde6a3e075f3bc5694c18e667.png");
}