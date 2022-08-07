using System.Collections.ObjectModel;
using System.IO;
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
                Binds = new ObservableCollection<SwitchableBind>()
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
                using (StreamReader sr = new StreamReader(ofd.FileName))
                using (JsonTextReader reader = new JsonTextReader(sr))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    var importedSettings = serializer.Deserialize<SwitchablePropertiesSettings>(reader);
                    Plugin.Settings.Properties.Clear();
                    foreach (var setting in importedSettings.Properties)
                        Plugin.Settings.Properties.Add(setting);
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
                using (StreamWriter sr = new StreamWriter(sfd.FileName))
                using (JsonTextWriter reader = new JsonTextWriter(sr))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(reader, Plugin.Settings);
                }
            }
        }
    }
}
