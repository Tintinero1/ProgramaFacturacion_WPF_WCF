using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Xml;

namespace FacturacionServiceLib
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]

    [XmlSerializerFormat]
    public interface FacturacionMethods
    {
        [OperationContract]
        bool ValidateCredentials(string username, string password);

        [OperationContract]
        bool UploadXML(XmlDocument xml);

        [OperationContract]
        factura InvoicesFromXML(string rfc);

        [OperationContract]
        DataSet DataTableInvoicesFromXML(string rfc);

        [OperationContract]
        DataSet DataTableInvoicesByPeriodXML(string rfc, string periodo);

        [OperationContract]
        bool DeleteXML_Record(string InvoiceNumber);

    }

    [DataContract]
    public class factura
    {
        [DataMember]
        public int TotalDeFacturas { get; set; }
        [DataMember]
        public string Nombre { get; set; }
        [DataMember]
        public double Total { get; set; }
        [DataMember]
        public double SubTotal { get; set; }
        [DataMember]
        public double TotalImpuestosTrasladados { get; set; }

        public factura()
        {
            TotalDeFacturas = 0;
        }

        public void addToTotal(double amount)
        {
            TotalDeFacturas++;
            Total += amount;
        }

        public void addToSubtotal(double amount)
        {
            SubTotal += amount;
        }

        public void addToTotalImpuestosTrasladados(double amount)
        {
            TotalImpuestosTrasladados += amount;
        }

    }

}
