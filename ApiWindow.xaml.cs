using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BybitBot
{
    public partial class ApiWindow : Window
    {
        MainWindow main;
        public int id = 0;

        public ApiWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            main = this.Owner as MainWindow;
            apiName.Focus();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            main.SaveApi(id, apiName.Text, apiKey.Text, apiSecret.Text, apiComission.Text, apiTest.IsChecked.Value);
            this.Close();
        }
    }
}
