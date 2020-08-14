using System;
using System.Collections.Generic;
using System.IO;
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
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using ClientFacturacion.ServiceReference1;

using Microsoft.Win32; //FileDialog

namespace ClientFacturacion.Views
{
    /// <summary>
    /// Interaction logic for Inicio.xaml
    /// </summary>
    public partial class Inicio : UserControl
    {
        FacturacionMethodsClient client;
        public Inicio()
        {
            InitializeComponent();
            client = new FacturacionMethodsClient();
        }

        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog factura = new OpenFileDialog();
            factura.Multiselect = true;
            factura.Filter = "XML|*.xml";
            factura.DefaultExt = ".xml";
            Nullable<bool> facturaOK = factura.ShowDialog();
            bool result = false;

            if (facturaOK == true)
            {
                string sFilenames = "";
                foreach (string sFilename in factura.FileNames)
                {
                    sFilenames += ";" + sFilename;
                }
                sFilenames = sFilenames.Substring(1); //delete first
                txtFileNames.Text = sFilenames;

                //  The stream will hold the results of opening the XML
                using (var myStream = factura.OpenFile())
                {
                    try
                    {
                        //  Successfully return the XML
                        XmlDocument parsedMyStream = new XmlDocument();
                        parsedMyStream.Load(myStream);

                        XElement xDoc = XElement.Load(new XmlNodeReader(parsedMyStream));
                        result = client.UploadXML(parsedMyStream);


                    }
                    catch (XmlException ex)
                    {
                        MessageBox.Show("The XML could not be read. " + ex);
                    }
                }


               

                System.Diagnostics.Debug.WriteLine("\n----- Se inserto el documento?: " + result + " -----");
            }
        }

        private void btnOpen_Copy_Click(object sender, RoutedEventArgs e)
        {
            factura fact = new factura();
            fact = client.InvoicesFromXML("AAAM9905055K9");
        }
    }
}
