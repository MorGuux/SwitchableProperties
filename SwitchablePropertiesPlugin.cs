using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;

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
            this.SaveCommonSettings("GeneralSettings", Settings);
        }

        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            return new SettingsControl(this);
        }

        public void Init(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("Starting plugin");

            // Load settings
            Settings = this.ReadCommonSettings("GeneralSettings", () => new SwitchablePropertiesSettings
            {
                Properties = new ObservableCollection<SwitchableProperty>()
            });

            if (Settings.Properties == null)
                Settings.Properties = new ObservableCollection<SwitchableProperty>();

            //Initialise properties to their default value (first bind value)
            _switchableProperties = new List<SwitchablePropertyContainer>();

            if (Settings.Properties.Count != 0)
            {
                foreach (SwitchableProperty property in Settings.Properties)
                {
                    if (property.Binds != null && property.Binds.Count != 0)
                        _switchableProperties.Add(new SwitchablePropertyContainer
                        {
                            PropertyValue = property.Binds[0].PropertyValue,
                            Property = property
                        });
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
                    this.AddAction($"{property.Property.PropertyName}_{bind.ActionName}", (a, b) =>
                    {
                        property.PropertyValue = bind.PropertyValue;
                        this.TriggerEvent($"{property.Property.PropertyName}Update");
                    });
                }
            }
        }
    }

    internal class SwitchablePropertyContainer
    {
        internal string PropertyValue;
        internal SwitchableProperty Property;
    }

    public class SwitchableProperty
    {
        public string PropertyName { get; set; }
        public ObservableCollection<SwitchableBind> Binds { get; set; }
    }
    public class SwitchableBind
    {
        public string ActionName { get; set; }
        public string PropertyValue { get; set; }
    }
}