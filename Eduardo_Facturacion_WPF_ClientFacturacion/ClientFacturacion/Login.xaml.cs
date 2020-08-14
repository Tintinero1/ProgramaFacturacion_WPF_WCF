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
using ClientFacturacion.ServiceReference1;

namespace ClientFacturacion
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        FacturacionMethodsClient client;
        public Login()
        {
            InitializeComponent();
            //Step 1: Create an instance of the WCF proxy.
            client = new FacturacionMethodsClient();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            bool result = false;
            result = client.ValidateCredentials(this.txtUsername.Text, this.txtPassword.Password);

            System.Diagnostics.Debug.WriteLine("\n----- El usuario es valido?: " +  result + " -----");

            var Page2 = new MainWindow(); //create your new form.
            Page2.Show(); //show the new form.
            this.Close(); //only if you want to close the current form.
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DragMove();
            } catch (Exception ex)
            { }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
