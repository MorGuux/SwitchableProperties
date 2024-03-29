﻿using GameReaderCommon;
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
using SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating;
using SimHub.Plugins.OutputPlugins.Dash.TemplatingCommon;

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
            catch (Exception)
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
            PluginManager.ClearActions(this.GetType());
            PluginManager.ClearProperties(this.GetType());

            //Initialise properties to their default value (first bind value)
            _switchableProperties = new List<SwitchablePropertyContainer>();

            if (Settings.Properties.Count != 0)
            {
                foreach (SwitchableProperty property in Settings.Properties)
                {
                    property.Plugin = this;

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
                    bind.Plugin = this;
                    bind.Property = property.Property;

                    if (bind.GetType() == typeof(SwitchableValueBind))
                    {
                        this.AddAction($"{property.Property.PropertyName}_{bind.ActionName}", (a, b) =>
                        {
                            if (property.IsPropertyDisabled()) return;

                            property.PropertyValue = ((SwitchableValueBind)bind).PropertyValue;
                            property.BindName = ((SwitchableValueBind)bind).ActionName;
                            this.TriggerEvent($"{property.Property.PropertyName}Update");
                        });
                    }
                    else if (bind.GetType() == typeof(SwitchableCyclerBind))
                    {
                        this.AddAction($"{property.Property.PropertyName}_{bind.ActionName}", (a, b) =>
                        {
                            if (property.IsPropertyDisabled()) return;

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
                            if (property.IsPropertyDisabled()) return;

                            property.PropertyValue = ((SwitchableToggleBind)(bind)).GetToggleValue(property.PropertyValue);
                            property.BindName = ((SwitchableToggleBind)bind).GetToggleName(property.BindName);
                            this.TriggerEvent($"{property.Property.PropertyName}Update");
                        });
                    }
                }
            }
        }

        internal void RenameInputMapTargets(string oldName, string newName)
        {
            var controlEditor = new SimHub.Plugins.UI.ControlsEditor() { Visibility = System.Windows.Visibility.Collapsed, ActionName = $"SwitchablePropertiesPlugin.{oldName}" };

            var inputList = controlEditor.Model.Triggers;

            foreach (var item in inputList)
            {
                item.Target = $"SwitchablePropertiesPlugin.{newName}";
            }
        }

        internal void DeleteInputMapTargets(string oldName)
        {
            var test = new SimHub.Plugins.UI.ControlsEditor() { Visibility = System.Windows.Visibility.Collapsed, ActionName = $"SwitchablePropertiesPlugin.{oldName}" };

            var inputList = test.Model.Triggers;

            foreach (var item in inputList)
            {
                /*Setting the Trigger and Target empty allows them to be bound again without prompting the user
                And SimHub will clean up the empty bind on the next reload*/
                item.Target = "";
                item.Trigger = "";
            }
        }
    }

    internal class SwitchablePropertyContainer
    {
        internal string PropertyValue;
        internal string BindName;
        private int _propertyIndex;
        internal SwitchableProperty Property;
        private NCalcEngineBase _engine = new NCalcEngineBase();

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

        /// <summary>
        /// Is the property actively disabled (based on the EnabledProperty expression)
        /// </summary>
        /// <returns>True if the property is disabled and cannot be modified</returns>
        public bool IsPropertyDisabled()
        {
            if (!Property.CanBeDisabled)
                return false;

            var result = _engine.ParseValue(Property.EnabledProperty);
            if (result is bool b) return !b;

            return false;
        }
    }

    public class SwitchableProperty : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _propertyName;

        private string _nameTextBoxValue; //used by the text box to allow modifying, while still knowing from what you modify

        private bool _canBeDisabled;

        public bool CanBeDisabled
        {
            get => _canBeDisabled;
            set
            {
                _canBeDisabled = value;
                OnPropertyChanged();
            }
        }

        private ExpressionValue _enabledProperty;

        public ExpressionValue EnabledProperty
        {
            get => _enabledProperty;
            set
            {
                _enabledProperty = value;
                OnPropertyChanged();
            }
        }

        public string PropertyName
        {
            get => _propertyName;
            set
            {
                _propertyName = value;
                OnPropertyChanged();

                _nameTextBoxValue = value;
                OnPropertyChanged("NameTextBoxValue");
            }
        }

        [JsonIgnore]
        public string NameTextBoxValue
        {
            get => _nameTextBoxValue;
            set
            {
                if (_nameTextBoxValue == value)
                    return;

                value = value.Trim();

                _nameTextBoxValue = value;
                OnPropertyChanged();

                if (Plugin != null)
                {
                    bool collides = false;

                    if (PropertyName != value)
                    {
                        if (Plugin.Settings.Properties.Any(item => item.PropertyName.ToLower().Equals(value.ToLower())))
                        {
                            collides = true;
                        }
                    }

                    if (collides)
                    {
                        BorderBrush = Brushes.Red;
                    }
                    else
                    {
                        BorderBrush = Brushes.LightGray;

                        if (PropertyName != value)
                        {
                            foreach (var item in Binds)
                            {
                                Plugin.RenameInputMapTargets($"{PropertyName}_{item.ActionName}", $"{value}_{item.ActionName}");
                            }
                        }

                        PropertyName = value;

                        Plugin.GenerateBinds();
                    }
                }
                else
                {
                    PropertyName = value;
                }
            }
        }

        private Brush _borderBrush = Brushes.LightGray;

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
        
        [JsonIgnore]
        internal SwitchablePropertiesPlugin Plugin { get; set; }
        
        public ObservableCollection<SwitchableBind> Binds { get; set; }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public abstract class SwitchableBind : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _actionName;

        private string _nameTextBoxValue; //used 

        public string ActionName
        {
            get => _actionName;
            set
            {
                _actionName = value;
                OnPropertyChanged();

                _nameTextBoxValue = value;
                OnPropertyChanged("NameTextBoxValue");
            }
        }

        [JsonIgnore]
        public string NameTextBoxValue
        {
            get => _nameTextBoxValue;
            set
            {
                if (_nameTextBoxValue == value)
                    return;

                value = value.Trim();

                _nameTextBoxValue = value;
                OnPropertyChanged();

                if (Plugin != null && Property != null)
                {
                    bool collides = false;

                    if (ActionName != value)
                    {
                        string fullActionName = ($"{Property.PropertyName}_{value}").ToLower();

                        foreach (var item in Plugin.Settings.Properties)
                        {
                            foreach (var bindings in item.Binds)
                            {
                                string cacheName = ($"{item.PropertyName}_{bindings.ActionName}").ToLower();

                                if (cacheName.Equals(fullActionName))
                                {
                                    collides = true;
                                    break;
                                }
                            }

                            if (collides)
                                break;
                        }
                    }

                    if (collides)
                    {
                        BorderBrush = Brushes.Red;
                    }
                    else
                    {
                        BorderBrush = Brushes.LightGray;

                        if (ActionName != value)
                            Plugin.RenameInputMapTargets($"{Property.PropertyName}_{this.ActionName}", $"{Property.PropertyName}_{value}");

                        ActionName = value;

                        Plugin.GenerateBinds();
                    }
                }
                else
                {
                    ActionName = value;
                }
            }
        }

        private Brush _borderBrush = Brushes.LightGray;

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

        [JsonIgnore]
        internal SwitchablePropertiesPlugin Plugin { get; set; }

        [JsonIgnore]
        internal SwitchableProperty Property { get; set; }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SwitchableValueBind : SwitchableBind
    {
        public string PropertyValue { get; set; }
    }

    public class SwitchableCyclerBind : SwitchableBind
    {
        public string Direction { get; set; }
    }

    public class SwitchableToggleBind : SwitchableBind
    {
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
}