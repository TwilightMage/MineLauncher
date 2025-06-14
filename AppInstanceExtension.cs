using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace MineLauncher;

public class AppInstanceExtension : MarkupExtension
{
    public string Path { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new Binding(Path)
        {
            Source = App.Instance
        };
    }

}