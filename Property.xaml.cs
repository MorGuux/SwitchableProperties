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

            binds.Add(new SwitchableValueBind() { ActionName =  $"{((SwitchableProperty)this.DataContext).PropertyName}_{binds.Count}"});
        }

        private void btnAddCyclerBind_Click(object sender, RoutedEventArgs e)
        {
            var binds = ((IList)pnlBinds.ItemsSource);

            if (binds.Count == 0)
            {
                //Generating a template
                binds.Add(new SwitchableValueBind() { ActionName = $"{((SwitchableProperty)this.DataContext).PropertyName}_{binds.Count}" });
                binds.Add(new SwitchableValueBind() { ActionName = $"{((SwitchableProperty)this.DataContext).PropertyName}_{binds.Count}" });
                binds.Add(new SwitchableCyclerBind() { ActionName = "Cycler", Direction = "Forward" });
            }
            else
            {
                binds.Add(new SwitchableCyclerBind() { ActionName = $"{((SwitchableProperty)this.DataContext).PropertyName}_{binds.Count}_Cycler" });
            }
        }

        private void btnAddToggleBind_Click(object sender, RoutedEventArgs e)
        {
            var binds = ((IList)pnlBinds.ItemsSource);

            if (binds.Count == 0)
            {
                //Generating a template
                binds.Add(new SwitchableValueBind() { ActionName = "Default" });
                binds.Add(new SwitchableToggleBind() { ActionName = "Toggle" });

            }
            else
            {
                binds.Add(new SwitchableToggleBind() { ActionName = $"{((SwitchableProperty)this.DataContext).PropertyName}_{binds.Count}_Toggle" });
            }
            
        }

        private void DeleteBind_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ((SwitchableProperty)this.DataContext).Binds.Remove(e.Parameter as SwitchableBind);
        }
    }
}
