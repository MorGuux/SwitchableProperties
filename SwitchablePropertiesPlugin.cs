using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SwitchableProperties
{
    [PluginDescription("Control multiple switchable SimHub properties using binds")]
    [PluginAuthor("Morgan Gardner (MorGuux)")]
    [PluginName("SwitchableProperties")]
    public class SwitchablePropertiesPlugin : IPlugin, IDataPlugin, IWPFSettingsV2
    {
        public SwitchablePropertiesSettings Settings;

        public PluginManager PluginManager { get; set; }

        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);

        public string LeftMenuTitle => "SwitchableProperties";

        private List<SwitchablePropertyContainer> _switchableProperties;

        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            //Not needed
        }

        public void End(PluginManager pluginManager)
        {
            CheckForCollisions(true);

            // Save settings
            File.WriteAllText(PluginManager.GetCommonStoragePath(this.GetType().Name + ".GeneralSettings.json"), 
                JsonConvert.SerializeObject(Settings, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                }));
        }

        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            return new SettingsControl(this);
        }

        public void Init(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("Starting plugin");

            // Load settings
            try
            {
                Settings = JsonConvert.DeserializeObject<SwitchablePropertiesSettings>(
                    File.ReadAllText(PluginManager.GetCommonStoragePath(this.GetType().Name + ".GeneralSettings.json")),
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto,
                        Formatting = Formatting.Indented,
                    });
            }
            catch (JsonSerializationException)
            {
                var oldSettings = JsonConvert.DeserializeObject<OldSwitchablePropertiesSettings>(
                        File.ReadAllText(
                            PluginManager.GetCommonStoragePath(this.GetType().Name + ".GeneralSettings.json")));
                Settings = new SwitchablePropertiesSettings();
                Settings.Properties = new ObservableCollection<SwitchableProperty>();
                foreach (var setting in oldSettings.Properties)
                {
                    Settings.Properties.Add(new SwitchableProperty
                    {
                        PropertyName = setting.PropertyName,
                        Binds = new ObservableCollection<SwitchableBind>(setting.Binds)
                    });
                }
            }
            catch (FileNotFoundException)
            {
                Settings = new SwitchablePropertiesSettings
                {
                    Properties = new ObservableCollection<SwitchableProperty>()
                };
            }

            if (Settings.Properties == null)
                Settings.Properties = new ObservableCollection<SwitchableProperty>();

            GenerateBinds();
        }

        internal void GenerateBinds()
        {
            //Initialise properties to their default value (first bind value)
            _switchableProperties = new List<SwitchablePropertyContainer>();

            if (Settings.Properties.Count != 0)
            {
                foreach (SwitchableProperty property in Settings.Properties)
                {
                    var propertyContainer = new SwitchablePropertyContainer();
                    var switchableValueBind = property.Binds
                        .OfType<SwitchableValueBind>()
                        .FirstOrDefault();
                    propertyContainer.PropertyValue = switchableValueBind?.PropertyValue;
                    propertyContainer.BindName = switchableValueBind?.ActionName;
                    propertyContainer.Property = property;
                    _switchableProperties.Add(propertyContainer);
                }
            }

            foreach (SwitchablePropertyContainer property in _switchableProperties)
            {
                // Declare a property available in the property list, this gets evaluated "on demand" (when shown or used in formulas)
                this.AttachDelegate($"{property.Property.PropertyName}", () => property.PropertyValue);
                this.AttachDelegate($"{property.Property.PropertyName}_ActiveBind", () => property.BindName);


                // Declare an event
                this.AddEvent($"{property.Property.PropertyName}Update");

                if (property.Property.Binds == null)
                    return;

                foreach (var bind in property.Property.Binds)
                {
                    if (bind.GetType() == typeof(SwitchableValueBind))
                    {
                        this.AddAction($"{property.Property.PropertyName}_{bind.ActionName}", (a, b) =>
                        {
                            property.PropertyValue = ((SwitchableValueBind)bind).PropertyValue;
                            property.BindName = ((SwitchableValueBind)bind).ActionName;
                            this.TriggerEvent($"{property.Property.PropertyName}Update");
                        });
                    }
                    else if (bind.GetType() == typeof(SwitchableCyclerBind))
                    {
                        this.AddAction($"{property.Property.PropertyName}_{bind.ActionName}", (a, b) =>
                        {
                            var direction = ((SwitchableCyclerBind)bind).Direction;
                            var nextBind = direction == "Forward" ? property.GetNextBind() : property.GetPreviousBind();

                            property.PropertyValue = nextBind.PropertyValue;
                            property.BindName = nextBind.ActionName;
                            this.TriggerEvent($"{property.Property.PropertyName}Update");
                        });
                    }
                    else if (bind.GetType() == typeof(SwitchableToggleBind))
                    {
                        this.AddAction($"{property.Property.PropertyName}_{bind.ActionName}", (a, b) =>
                        {
                            property.PropertyValue = ((SwitchableToggleBind)(bind)).GetToggleValue(property.PropertyValue);
                            property.BindName = ((SwitchableToggleBind)bind).GetToggleName(property.BindName);
                            this.TriggerEvent($"{property.Property.PropertyName}Update");
                        });
                    }
                }
            }
        }

        internal bool CheckForCollisions(bool fixDublicate)
        {
            if (Settings.Properties.Count == 0)
                return false;

            bool existDublicate = false;

            HashSet<string> names = new HashSet<string>();

            foreach (SwitchableProperty property in Settings.Properties)
            {
                if (names.Contains(property.PropertyName))
                {
                    existDublicate = true;

                    if (fixDublicate)
                    {
                        int i;
                        for (i = 0; names.Contains($"{names.Contains(property.PropertyName)}_{i}") && i < Int32.MaxValue; i++) ;

                        if (i != Int32.MaxValue)
                            property.PropertyName = $"{property.PropertyName}_{i}";
                    }
                    else
                    {
                        property.BorderBrush = Brushes.Red;
                    }
                }
                else
                {
                    names.Add(property.PropertyName);
                    property.BorderBrush = Brushes.Black;
                }
            }

            return existDublicate;
        }
    }

    internal class SwitchablePropertyContainer
    {
        internal string PropertyValue;
        internal string BindName;
        private int _propertyIndex;
        internal SwitchableProperty Property;

        internal SwitchableValueBind GetNextBind()
        {
            _propertyIndex = GetIndexOfBind();

            _propertyIndex++;
            if ((_propertyIndex) > Property.Binds
                    .OfType<SwitchableValueBind>()
                    .Count() - 1)
            {
                _propertyIndex = 0;
            }

            return GetBindValueAt(_propertyIndex);
        }

        internal SwitchableValueBind GetPreviousBind()
        {
            _propertyIndex = GetIndexOfBind();

            _propertyIndex--;
            if ((_propertyIndex) < 0)
            {
                _propertyIndex = Property.Binds
                    .OfType<SwitchableValueBind>()
                    .Count() - 1;
            }

            return GetBindValueAt(_propertyIndex);
        }

        private SwitchableValueBind GetBindValueAt(int index)
        {
            return Property.Binds
                .OfType<SwitchableValueBind>()
                .ElementAt(index);
        }

        private int GetIndexOfBind()
        {
            var activeBind = Property.Binds
                .OfType<SwitchableValueBind>()
                .FirstOrDefault(x => x.PropertyValue == PropertyValue);

            return Property.Binds
                .OfType<SwitchableValueBind>()
                .ToList()
                .IndexOf(activeBind);
        }
    }

    public class SwitchableProperty : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _propertyName;

        public string PropertyName
        {
            get => _propertyName;
            set
            {
                _propertyName = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public Brush BorderBrush
        {
            get => _borderBrush;
            set
            {
                _borderBrush = value;
                OnPropertyChanged();
            }
        }

        private Brush _borderBrush = Brushes.Black;
        public ObservableCollection<SwitchableBind> Binds { get; set; }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public abstract class SwitchableBind
    {
        public abstract string ActionName { get; set; }
    }

    public class SwitchableValueBind : SwitchableBind
    {
        public override string ActionName { get; set; }
        public string PropertyValue { get; set; }
    }

    public class SwitchableCyclerBind : SwitchableBind
    {
        public override string ActionName { get; set; }
        public string Direction { get; set; }
    }

    public class SwitchableToggleBind : SwitchableBind
    {
        public override string ActionName { get; set; }
        public string PropertyValue { get; set; }
        private string _lastPropertyValue;
        private string _lastBind;
        public string GetToggleValue(string oldValue)
        {
            if (oldValue == PropertyValue)
            {
                string newValue = _lastPropertyValue;
                _lastPropertyValue = PropertyValue;
                return newValue;
            }
            else
            {
                _lastPropertyValue = oldValue;
                return PropertyValue;
            }
        }
        public string GetToggleName(string oldBind)
        {
            if (oldBind == ActionName)
            {
                string newValue = _lastBind;
                _lastBind = ActionName;
                return newValue;
            }
            else
            {
                _lastBind = oldBind;
                return ActionName;
            }
        }
    }

    //USED FOR CONVERTING V1 SETTINGS into V1.1+
    internal class OldSwitchablePropertiesSettings
    {
        public ObservableCollection<OldSwitchableProperty> Properties { get; set; }
    }

    internal class OldSwitchableProperty
    {
        public string PropertyName { get; set; }
        public ObservableCollection<SwitchableValueBind> Binds { get; set; }
    }
}