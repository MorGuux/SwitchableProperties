using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Newtonsoft.Json;
using SimHub.Plugins;
using SimHub.Plugins.UI;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using UserControl = System.Windows.Controls.UserControl;

namespace SwitchableProperties
{
    /// <summary>
    /// Logique d'interaction pour SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        public SwitchablePropertiesPlugin Plugin { get; }
        public static RoutedCommand DeletePropertyCommand = new RoutedCommand();

        public SettingsControl()
        {
            InitializeComponent();
        }

        public SettingsControl(SwitchablePropertiesPlugin plugin) : this()
        {
            this.Plugin = plugin;
            pnlProperties.ItemsSource = Plugin.Settings.Properties;
        }

        private void btnAddProperty_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Plugin.Settings.Properties.Add(new SwitchableProperty
            {
                Binds = new ObservableCollection<SwitchableBind>(),
                Plugin = this.Plugin
            });
        }

        private void DeleteProperty_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Plugin.Settings.Properties.Remove(e.Parameter as SwitchableProperty);
        }

        private void btnImport_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Browse for settings",
                DefaultExt = ".json",
                Filter = "JSON files (*.json)|*.json",
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    var importedSettings = JsonConvert.DeserializeObject<SwitchablePropertiesSettings>(File.ReadAllText(ofd.FileName),
                        new JsonSerializerSettings
                        {
                            TypeNameHandling = TypeNameHandling.Auto,
                            Formatting = Formatting.Indented,
                        });
                    Plugin.Settings.Properties.Clear();
                    foreach (var setting in importedSettings.Properties)
                        Plugin.Settings.Properties.Add(setting);
                }
                catch (JsonSerializationException)
                {
                    var importedSettings = JsonConvert.DeserializeObject<OldSwitchablePropertiesSettings>(File.ReadAllText(ofd.FileName));
                    Plugin.Settings.Properties.Clear();
                    foreach (var setting in importedSettings.Properties)
                    {
                        Plugin.Settings.Properties.Add(new SwitchableProperty
                        {
                            PropertyName = setting.PropertyName,
                            Binds = new ObservableCollection<SwitchableBind>(setting.Binds)
                        });
                    }
                }
            }
        }

        private void btnExport_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Title = "Browse for settings save location",
                DefaultExt = ".json",
                Filter = "JSON files (*.json)|*.json",
                CheckPathExists = true
            };

            if (sfd.ShowDialog() == true)
            {
                File.WriteAllText(sfd.FileName,
                    JsonConvert.SerializeObject(Plugin.Settings, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto
                    }));
            }
        }
    }
}
