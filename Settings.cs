using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace MineLauncher
{
    public enum Language
    {
        System,
        Russian,
        English
    }
    
    public class LanguageDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Language language)
            {
                return language switch
                {
                    Language.System => culture.TwoLetterISOLanguageName switch
                    {
                        "ru" => "Системный",
                        _ => "System"
                    },
                    Language.Russian => "Русский",
                    Language.English => "English",
                    _ => language.ToString()
                };
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    
    public class SerializableSetting : Attribute
    { }
    
    public class Settings : INotifyPropertyChanged
    {
        private Language _language = Language.System;
        [SerializableSetting]
        public Language Language
        {
            get => _language;
            set {
                if (SetField(ref _language, value))
                {
                    LanguageChanged?.Invoke();
                    Save();
                }
            }
        }
        public event Action LanguageChanged;
        
        public Language GetUsedLanguage()
        {
            if (Language == Language.System)
            {
                return System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName switch
                {
                    "ru" => Language.Russian,
                    "en" => Language.English,
                    _ => Language.English
                };
            }
            
            return Language;
        }
        
        private string _installDir;
        [SerializableSetting]
        public string InstallDir
        {
            get => _installDir;
            set {
                if (SetField(ref _installDir, value))
                {
                    InstallDirChanged?.Invoke();
                    Save();
                }
            }
        }
        public event Action InstallDirChanged;
        
        private string _repo;
        [SerializableSetting]
        public string Repo
        {
            get => _repo;
            set
            {
                if (SetField(ref _repo, value))
                {
                    RepoChanged?.Invoke();
                    Save();
                }
            }
        }
        public event Action RepoChanged;
        
        private string _javaPath;
        [SerializableSetting]
        public string JavaPath
        {
            get => _javaPath;
            set
            {
                if (SetField(ref _javaPath, value))
                {
                    JavaPathChanged?.Invoke();
                    Save();
                }
            }
        }
        public event Action JavaPathChanged;
        
        private int _minJavaSizeMb = 4 * 1024;
        [SerializableSetting]
        public int MinJavaSizeMb
        {
            get => _minJavaSizeMb;
            set
            {
                if (SetField(ref _minJavaSizeMb, value))
                {
                    MinJavaSizeChanged?.Invoke();
                    Save();
                }
            }
        }
        public event Action MinJavaSizeChanged;
        
        private int _maxJavaSizeMb = 4 * 1024;
        [SerializableSetting]
        public int MaxJavaSizeMb
        {
            get => _maxJavaSizeMb;
            set
            {
                if (SetField(ref _maxJavaSizeMb, value))
                {
                    MaxJavaSizeChanged?.Invoke();
                    Save();
                }
            }
        }
        public event Action MaxJavaSizeChanged;

        IEnumerable<PropertyInfo> GetSerializableProperties() => GetType().GetProperties()
            .Where(prop => prop.GetCustomAttributes(typeof(SerializableSetting), false).Any());

        public void Load()
        {
            // Get the directory where the executable is located
            string exePath = Assembly.GetExecutingAssembly().Location;
            string exeDir = Path.GetDirectoryName(exePath);
            string iniPath = Path.Combine(exeDir, "settings.ini");

            if (File.Exists(iniPath))
            {
                List<PropertyInfo> props = GetSerializableProperties().ToList();
                
                // Read settings from INI file
                var lines = File.ReadAllLines(iniPath);
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        var prop = props.FirstOrDefault(f => f.Name == parts[0].Trim());
                        if (prop != null)
                        {
                            object convertedValue;
                            var targetType = prop.PropertyType;
    
                            if (targetType == typeof(string))
                            {
                                convertedValue = parts[1].Trim();
                            }
                            else
                            {
                                // Use type converter to handle numeric types
                                var converter = TypeDescriptor.GetConverter(targetType);
                                convertedValue = converter.ConvertFromString(parts[1].Trim());
                            }

                            prop.SetValue(this, convertedValue);
                        }
                    }
                }
            }
        }

        public void Save()
        {
            string exePath = Assembly.GetExecutingAssembly().Location;
            string exeDir = Path.GetDirectoryName(exePath);
            string iniPath = Path.Combine(exeDir, "settings.ini");

            var lines = GetSerializableProperties().Select(prop => $"{prop.Name}={prop.GetValue(this)}");
            File.WriteAllLines(iniPath, lines);
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
}