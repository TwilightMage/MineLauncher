using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MineLauncher
{
    public class SerializableSetting : Attribute
    { }
    
    public class Settings : INotifyPropertyChanged
    {
        private string _installDir;
        [SerializableSetting]
        public string InstallDir
        {
            get => _installDir;
            set {
                if (SetField(ref _installDir, value, InstallDir))
                {
                    OnInstallDirChanged?.Invoke();
                    Save();
                }
            }
        }
        public event Action OnInstallDirChanged;
        
        private string _repo;
        [SerializableSetting]
        public string Repo
        {
            get => _repo;
            set
            {
                if (SetField(ref _repo, value))
                {
                    OnRepoChanged?.Invoke();
                    Save();
                }
            }
        }
        public event Action OnRepoChanged;
        
        private string _java;
        [SerializableSetting]
        public string Java
        {
            get => _java;
            set
            {
                if (SetField(ref _java, value))
                {
                    OnJavaChanged?.Invoke();
                    Save();
                }
            }
        }
        public event Action OnJavaChanged;
        
        private int _minJavaSizeMb = 4 * 1024;
        [SerializableSetting]
        public int MinJavaSizeMb
        {
            get => _minJavaSizeMb;
            set
            {
                if (SetField(ref _minJavaSizeMb, value))
                {
                    OnMinJavaSizeChanged?.Invoke();
                    Save();
                }
            }
        }
        public event Action OnMinJavaSizeChanged;
        
        private int _maxJavaSizeMb = 4 * 1024;
        [SerializableSetting]
        public int MaxJavaSizeMb
        {
            get => _maxJavaSizeMb;
            set
            {
                if (SetField(ref _maxJavaSizeMb, value))
                {
                    OnMaxJavaSizeChanged?.Invoke();
                    Save();
                }
            }
        }
        public event Action OnMaxJavaSizeChanged;

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