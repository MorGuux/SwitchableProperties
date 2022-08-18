using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SwitchableProperties
{
    /// <summary>
    /// Interaction logic for Property.xaml
    /// </summary>
    public partial class Property : UserControl
    {
        public static RoutedCommand DeleteBindCommand = new RoutedCommand();

        public Property()
        {
            InitializeComponent();
        }

        private void btnAddBind_Click(object sender, RoutedEventArgs e)
        {
            var binds = ((IList)pnlBinds.ItemsSource);
            var property = ((SwitchableProperty)this.DataContext);

            binds.Add(new SwitchableValueBind() { ActionName =  $"{property.PropertyName}_{binds.Count}", Plugin = property.Plugin, Property = property });

            property.Plugin.GenerateBinds();
        }

        private void btnAddCyclerBind_Click(object sender, RoutedEventArgs e)
        {
            var binds = ((IList)pnlBinds.ItemsSource);
            var property = ((SwitchableProperty)this.DataContext);

            if (binds.Count == 0)
            {
                //Generating a template
                binds.Add(new SwitchableValueBind() { ActionName = $"{property.PropertyName}_{binds.Count}", Plugin = property.Plugin, Property = property });
                binds.Add(new SwitchableValueBind() { ActionName = $"{property.PropertyName}_{binds.Count}", Plugin = property.Plugin, Property = property });
                binds.Add(new SwitchableCyclerBind() { ActionName = "Cycler", Direction = "Forward", Plugin = property.Plugin, Property = property });
            }
            else
            {
                binds.Add(new SwitchableCyclerBind() { ActionName = $"{((SwitchableProperty)this.DataContext).PropertyName}_{binds.Count}_Cycler", Plugin = property.Plugin, Property = property });
            }

            property.Plugin.GenerateBinds();
        }

        private void btnAddToggleBind_Click(object sender, RoutedEventArgs e)
        {
            var binds = ((IList)pnlBinds.ItemsSource);
            var property = ((SwitchableProperty)this.DataContext);

            if (binds.Count == 0)
            {
                //Generating a template
                binds.Add(new SwitchableValueBind() { ActionName = "Default", Plugin = property.Plugin, Property = property });
                binds.Add(new SwitchableToggleBind() { ActionName = "Toggle", Plugin = property.Plugin, Property = property });

            }
            else
            {
                binds.Add(new SwitchableToggleBind() { ActionName = $"{property.PropertyName}_{binds.Count}_Toggle", Plugin = property.Plugin, Property = property });
            }

            property.Plugin.GenerateBinds();
        }

        private void DeleteBind_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ((SwitchableProperty)this.DataContext).Binds.Remove(e.Parameter as SwitchableBind);
        }
    }
}
