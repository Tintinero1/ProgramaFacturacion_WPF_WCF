using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClientFacturacion.ServiceReference1;

namespace ClientFacturacion.Views
{
    /// <summary>
    /// Interaction logic for ConsultaFacturas.xaml
    /// </summary>
    public partial class ConsultaFacturas : UserControl
    {
        FacturacionMethodsClient client;
        public ConsultaFacturas()
        {
            InitializeComponent();
            client = new FacturacionMethodsClient();
            GetAllInvoices();
        }

        public void GetAllInvoices()
        {
            string RFC = "AAAM9905055K9";
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();

            factura fact = new factura();
            fact = client.InvoicesFromXML(RFC);
            lbTotal.Content = fact.Total.ToString();
            lbTotalDeImpuestos.Content = fact.TotalImpuestosTrasladados.ToString();
            lbSubtotal.Content = fact.SubTotal.ToString();
            lbNumeroDeFacturas.Content = fact.TotalDeFacturas.ToString();
            lbRFC.Content = RFC;
            lbNombre.Content = "Marc Albrand Aguirre";

            ds = client.DataTableInvoicesFromXML(RFC);
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                Debug.WriteLine("ESTO ES EL USUARIO: " + row["Nombre"]);
            }
            ds.Tables[0].Columns.Remove("xml");
            dgvFacturas.DataContext = ds.Tables[0].DefaultView;
            //dgvFacturas.Columns[0].Visibility = Visibility.Collapsed;
        }

        private void cbPeriodos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool MonthNotChoosed = false;
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();

            int Month = cbPeriodos.SelectedIndex;
            string RealMonth = "";
            switch (Month)
            {
                case 0:
                    {
                        RealMonth = "Enero";
                        break;
                    }
                case 1:
                    {
                        RealMonth = "Febrero";
                        break;
                    }
                case 2:
                    {
                        RealMonth = "Marzo";
                        break;
                    }
                case 3:
                    {
                        RealMonth = "Abril";
                        break;
                    }
                case 4:
                    {
                        RealMonth = "Mayo";
                        break;
                    }
                case 5:
                    {
                        RealMonth = "Junio";
                        break;
                    }
                case 6:
                    {
                        RealMonth = "Julio";
                        break;
                    }
                case 7:
                    {
                        RealMonth = "Agosto";
                        break;
                    }
                case 8:
                    {
                        RealMonth = "Septiembre";
                        break;
                    }
                case 9:
                    {
                        RealMonth = "Octubre";
                        break;
                    }
                case 10:
                    {
                        RealMonth = "Noviembre";
                        break;
                    }
                case 11:
                    {
                        RealMonth = "Diciembre";
                        break;
                    }
                default:
                    {
                        MonthNotChoosed = true;
                        break;
                    }
            }
            if (!MonthNotChoosed)
            {
                Debug.WriteLine("THIS IS MONTH: " + RealMonth);
                ds = client.DataTableInvoicesByPeriodXML("AAAM9905055K9", RealMonth);
                dgvFacturas.DataContext = ds.Tables[0].DefaultView;
            }
            else
            {
                GetAllInvoices();
            }

        }

        private void btnBorrar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool result = false;
                DataRowView row = (DataRowView)dgvFacturas.SelectedItems[0];

                Debug.WriteLine("LOLOLOLO: " + row["Numero de Factura"]);
                result = client.DeleteXML_Record(row["Numero de Factura"].ToString());
                GetAllInvoices();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No hay factura seleccionada para borrar");
            }
            

        }

    }

    public class rowFactura
    {
        public string Nombre { get; set; }
        public string rfc { get; set; }
        public string folio { get; set; }
        public string periodo { get; set; }
    }
}
