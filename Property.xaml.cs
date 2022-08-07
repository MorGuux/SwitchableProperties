using System;
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
            ((IList<SwitchableBind>)pnlBinds.ItemsSource).Add(new SwitchableBind());
        }

        private void DeleteBind_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ((SwitchableProperty)this.DataContext).Binds.Remove(e.Parameter as SwitchableBind);
        }
    }
}
