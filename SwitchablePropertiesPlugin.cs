using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using Newtonsoft.Json;

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
                    propertyContainer.Property = property;
                    _switchableProperties.Add(propertyContainer);
                }
            }

            foreach (SwitchablePropertyContainer property in _switchableProperties)
            {
                // Declare a property available in the property list, this gets evaluated "on demand" (when shown or used in formulas)
                this.AttachDelegate($"{property.Property.PropertyName}", () => property.PropertyValue);

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
                            this.TriggerEvent($"{property.Property.PropertyName}Update");
                        });
                    }
                    else if (bind.GetType() == typeof(SwitchableCyclerBind))
                    {
                        this.AddAction($"{property.Property.PropertyName}_{bind.ActionName}", (a, b) =>
                        {
                            var direction = ((SwitchableCyclerBind)bind).Direction;
                            if(direction == "Forward")
                                property.PropertyValue = property.GetNextBindValue();
                            else
                                property.PropertyValue = property.GetPreviousBindValue();
                            this.TriggerEvent($"{property.Property.PropertyName}Update");
                        });
                    }
                    else if (bind.GetType() == typeof(SwitchableToggleBind))
                    {
                        this.AddAction($"{property.Property.PropertyName}_{bind.ActionName}", (a, b) =>
                        {
                            property.PropertyValue = property.GetToggleValue(((SwitchableToggleBind)bind).PropertyValue);
                            this.TriggerEvent($"{property.Property.PropertyName}Update");
                        });
                    }
                }
            }
        }
    }

    internal class SwitchablePropertyContainer
    {
        internal string PropertyValue;
        private string _lastPropertyValue;
        private int _propertyIndex;
        internal SwitchableProperty Property;

        internal string GetNextBindValue()
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

        internal string GetPreviousBindValue()
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

        private string GetBindValueAt(int index)
        {
            return Property.Binds
                .OfType<SwitchableValueBind>()
                .ElementAt(index)
                .PropertyValue;
        }

        private int GetIndexOfBind()
        {
            var activeBind = Property.Binds
                .OfType<SwitchableValueBind>()
                .FirstOrDefault(x => x.PropertyValue == PropertyValue);

            return Property.Binds
                .IndexOf(activeBind);
        }

        internal string GetToggleValue(string bindValue)
        {
            if (bindValue == PropertyValue)
            {
                PropertyValue = _lastPropertyValue;
            }
            else
            {
                _lastPropertyValue = PropertyValue;
                PropertyValue = bindValue;
            }
            return PropertyValue;
        }
    }

    public class SwitchableProperty
    {
        public string PropertyName { get; set; }
        public ObservableCollection<SwitchableBind> Binds { get; set; }
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