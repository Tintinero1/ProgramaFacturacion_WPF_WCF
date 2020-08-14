using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.IO;
using System.Configuration;
using System.Xml;
using System.Data.SqlTypes;
using System.Diagnostics;

namespace FacturacionServiceLib
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in both code and config file together.
    public class Service1 : FacturacionMethods
    {
        SqlConnection conn;
        public Service1()
        {
            string host = Dns.GetHostName();

            conn = new SqlConnection("server =  " + host + "\\SQLEXPRESS01; database=Facturacion; integrated security = true");
        }


        public bool ValidateCredentials(string username, string password)
        {
            bool result = false;
            List<string> parametros = new List<string>();
            parametros.Add(username);
            parametros.Add(password);

            int number = ExecSPReturnInt("Login_ValidateCredentials", parametros);
            if (number == 1)
                result = true;
            else
                result = false;

            return result;
        }

        public bool UploadXML(System.Xml.XmlDocument xml)
        {
            bool response = true;
            List<string> attributes = new List<string>();
            //XmlDocument xml = new XmlDocument();
            //xml.Load(xml2);
            attributes = Extract_RFC_Nombre_FromXML(xml);

            string RFC_Emisor = attributes[0].ToString();
            string Nombre_Emisor = attributes[1].ToString();
            string folio = attributes[2].ToString();

            attributes = Extract_Total_IVA_Subtotal_Folio_FromXML(xml);
            string periodo = attributes[4];
            periodo = FormatPeriod(periodo);

            SqlCommand cmd = new SqlCommand("[AltaFactura]");
            cmd.Connection = conn;
            cmd.CommandType = CommandType.StoredProcedure;
            

            cmd.Parameters.AddWithValue("@Nombre", Nombre_Emisor);
            cmd.Parameters.AddWithValue("@RFC", RFC_Emisor);
            cmd.Parameters.AddWithValue("@folio_factura", folio);
            cmd.Parameters.AddWithValue("@periodo", periodo);
            cmd.Parameters.Add(
               new SqlParameter("@xml", SqlDbType.Xml)
               {
                   Value = new SqlXml(new XmlTextReader(xml.InnerXml
                                   , XmlNodeType.Document, null))
               });
            conn.Open();
            cmd.ExecuteNonQuery();
            conn.Close();

            return response;
        }

        public factura InvoicesFromXML(string rfc)
        {
            factura fact = new factura();
            List<string> attributes = new List<string>();
            attributes.Add(rfc);
            DataTable dt = new DataTable();
            string sqlxml = "";

            dt = ExecSP("ConsultaFactura", attributes);

            foreach(DataRow row in dt.Rows)
            {
                XmlDocument xml = new XmlDocument();
                sqlxml = row["xml"].ToString();
                System.Diagnostics.Debug.WriteLine("ESTO ES MI XML: " + sqlxml);

                xml.LoadXml(sqlxml);
                attributes = Extract_Total_IVA_Subtotal_Folio_FromXML(xml);
                fact.addToTotal(Convert.ToDouble(attributes[0]));
                fact.addToSubtotal(Convert.ToDouble(attributes[1]));
                fact.addToTotalImpuestosTrasladados(Convert.ToDouble(attributes[2]));
            }
            Debug.WriteLine("------ Objecto Factura ------");
            Debug.WriteLine("Total facturas: " + fact.TotalDeFacturas);
            Debug.WriteLine("Total de pesos en facturas: " + fact.Total);
            Debug.WriteLine("Subtotal de pesos en facturas: " + fact.SubTotal);
            Debug.WriteLine("Total de IVA en facturas: " + fact.TotalImpuestosTrasladados);

            return fact;
        }

        [OperationBehavior]
        public DataSet DataTableInvoicesFromXML(string RFC)
        {
            DataSet ds = new DataSet();
            DataTable dt = new DataTable("Facturas");
            List<string> attributes = new List<string>();
            attributes.Add(RFC);
            dt = ExecSP("ConsultaFactura", attributes);

            foreach(DataRow row in dt.Rows)
            {
                Debug.WriteLine("ESTO ES EL USUARIO: " + row["Nombre"]);
            }

            ds.Tables.Add(dt);

            return ds;
        }

        [OperationBehavior]
        public DataSet DataTableInvoicesByPeriodXML(string rfc, string periodo)
        {
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            List<string> attributes = new List<string>();
            attributes.Add(rfc);
            attributes.Add(periodo);

            dt = ExecSP("ConsultaFacturaPorPeriodo", attributes);
            ds.Tables.Add(dt);
            return ds;
        }

        public List<string> Extract_RFC_Nombre_FromXML(XmlDocument xml)
        {
            List<string> attributes = new List<string>();
            string RFC_Emisor = "";
            string Nombre_Emisor = "";
            string Folio = "";
            int counter = 0;
            int attCounter = 0;

            List<string> node = new List<string>();

            // Load the XML file.
            XmlDocument doc = new XmlDocument();
            doc = xml;

            //var xpath = "/item/*[local-name()='Emisor']";
            var xpath = "//*[@Rfc]";
            var xpathFolio = "//*[@Folio]";
            XmlNodeList result = doc.SelectNodes(xpath);
            XmlNodeList resultFolio = doc.SelectNodes(xpathFolio);

            foreach (XmlNode att in result)
            {
                counter++;
                if (counter == 1)
                {
                    foreach (XmlAttribute a in att.Attributes)
                    {
                        Debug.WriteLine("ESTO ES EL NOMBRE: " + a.Name);
                        Debug.WriteLine("ESTO ES EL VALOR: " + a.Value);
                        if (a.Name == "Rfc")
                        {
                            RFC_Emisor = a.Value;
                        } else if (a.Name == "Nombre")
                        {
                            Nombre_Emisor = a.Value;
                        }
                    }
                }
            }
            counter = 0;
            foreach (XmlNode att in resultFolio)
            {
                counter++;
                if (counter == 1)
                {
                    foreach (XmlAttribute a in att.Attributes)
                    {
                        if (a.Name == "Folio")
                        {
                            Folio = a.Value;
                        }
                    }
                }
            }
            counter = 0;

            attributes.Add(RFC_Emisor);
            attributes.Add(Nombre_Emisor);
            attributes.Add(Folio);
            //Console.WriteLine(result.Count);

            return attributes;
        }

        public List<string> Extract_Total_IVA_Subtotal_Folio_FromXML(XmlDocument xml)
        {
            List<string> attributes = new List<string>();
            string Total = "";
            string SubTotal = "";
            string TotalImpuestosTrasladados = "";
            string Folio = "";
            string Periodo = "";
            int counter = 0;

            List<string> node = new List<string>();

            // Load the XML file.
            XmlDocument doc = new XmlDocument();
            doc = xml;

            var xpathTotal = "//*[@Total]";
            var xpathSubTotal = "//*[@SubTotal]";
            var xpathTotalImpuestosTrasladados = "//*[@TotalImpuestosTrasladados]";
            var xpathFolio = "//*[@Folio]";
            var xpathPeriodo = "//*[@Fecha]";
            XmlNodeList resultTotal = doc.SelectNodes(xpathTotal);
            XmlNodeList resultSubTotal = doc.SelectNodes(xpathSubTotal);
            XmlNodeList resultxpathTotalImpuestosTrasladados = doc.SelectNodes(xpathTotalImpuestosTrasladados);
            XmlNodeList resultFolio = doc.SelectNodes(xpathFolio);
            XmlNodeList resultPeriodo = doc.SelectNodes(xpathPeriodo);

            foreach (XmlNode att in resultTotal)
            {
                counter++;
                if (counter == 1)
                {
                    foreach (XmlAttribute a in att.Attributes)
                    {
                        if (a.Name == "Total")
                        {
                            Total = a.Value;
                        }
                    }
                }
            }
            counter = 0;
            foreach (XmlNode att in resultSubTotal)
            {
                counter++;
                if (counter == 1)
                {
                    foreach (XmlAttribute a in att.Attributes)
                    {
                        if (a.Name == "SubTotal")
                        {
                            SubTotal = a.Value;
                        }
                    }
                }
            }
            counter = 0;
            foreach (XmlNode att in resultxpathTotalImpuestosTrasladados)
            {
                counter++;
                if (counter == 1)
                {
                    foreach (XmlAttribute a in att.Attributes)
                    {
                        if (a.Name == "TotalImpuestosTrasladados")
                        {
                            TotalImpuestosTrasladados = a.Value;
                        }
                    }
                }
            }
            counter = 0;
            foreach (XmlNode att in resultFolio)
            {
                counter++;
                if (counter == 1)
                {
                    foreach (XmlAttribute a in att.Attributes)
                    {
                        if (a.Name == "Folio")
                        {
                            Folio = a.Value;
                        }
                    }
                }
            }
            counter = 0;
            foreach (XmlNode att in resultPeriodo)
            {
                counter++;
                if (counter == 1)
                {
                    foreach (XmlAttribute a in att.Attributes)
                    {
                        if (a.Name == "Fecha")
                        {
                            Periodo = a.Value;
                        }
                    }
                }
            }
            counter = 0;

            attributes.Add(Total);
            attributes.Add(SubTotal);
            attributes.Add(TotalImpuestosTrasladados);
            attributes.Add(Folio);
            attributes.Add(Periodo);

            Debug.WriteLine("Total: " + attributes[0]);
            Debug.WriteLine("SubTotal: " + attributes[1]);
            Debug.WriteLine("Total Impuestos Trasladados: " + attributes[2]);
            Debug.WriteLine("Numero de Folio: " + attributes[3]);
            Debug.WriteLine("Periodo: " + attributes[4]);

            return attributes;
        }

        public int ExecSPReturnInt(string querySP, List<string> parametros)
        {
            System.Diagnostics.Debug.WriteLine("\n----- Intentado ejecutar SP: " + querySP + " -----");
            string SPResult = "";
            bool SPResultDetected = false;
            bool intDetected = false;

            int i = 0;
            int a = 0;
            int ResultFromSP = 0;
            // Esta lista contiene los nombres de los parametros de cualquier SP que mandemos llamar.
            List<string> NombreParametros = new List<string>();

            SqlCommand cmd = new SqlCommand(querySP, conn);
            cmd.CommandType = CommandType.StoredProcedure;

            conn.Open();
            // Obtiene los parametros del SP y llena la lista de cmd
            SqlCommandBuilder.DeriveParameters(cmd);

            foreach (SqlParameter p in cmd.Parameters)
            {
                // Nos saltamos el primer parametro por defecto
                if (i != 0)
                {
                    System.Diagnostics.Debug.WriteLine("Leyendo parametro: " + p.ParameterName);
                    NombreParametros.Add(p.ParameterName.ToString());
                }
                i++;
            }
            i = 0;
            cmd.Parameters.Clear();

            // Recorremos cada uno de los parametros que agregamos anteriormente para agregar los que
            // realmente necesitamos a cmd.Parameters.
            foreach (string p in NombreParametros)
            {
                if (p == "@mensaje")
                {
                    cmd.Parameters.Add(p, SqlDbType.VarChar, 150);
                    cmd.Parameters[p].Direction = ParameterDirection.Output;
                    SPResultDetected = true;
                    System.Diagnostics.Debug.WriteLine("Se agrega: " + p + ", " + "' '");
                }
                else
                {
                    // Detecta si el parametro que optuvimos puede ser considerado como int o string.
                    intDetected = int.TryParse(parametros[i], out a);
                    if (intDetected)
                    {
                        cmd.Parameters.AddWithValue(p.ToString(), a);
                        System.Diagnostics.Debug.WriteLine("Se agrega INT: " + p + ", " + parametros[i]);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue(p.ToString(), parametros[i]);
                        System.Diagnostics.Debug.WriteLine("Se agrega: " + p + ", " + parametros[i]);
                    }
                }

                i++;
            }

            // Agregamos un parametro OUTPUT para devolver un entero de SP's especiales.
            // Ojo: Necesitas tener un return(@ParameterReturnINT) en tu sp con un valor INT valido.
            var ParameterReturnINT = cmd.Parameters.Add("@ParameterReturnINT", SqlDbType.Int);
            ParameterReturnINT.Direction = ParameterDirection.ReturnValue;

            cmd.ExecuteReader();
            conn.Close();

            // Guardamos el dato de los SP en una variable int.
            ResultFromSP = (int)ParameterReturnINT.Value;

            // Si detecta un mensaje lo guarda y lo despliega en consola.
            if (SPResultDetected)
            {
                SPResult = cmd.Parameters["@mensaje"].Value.ToString();
                System.Diagnostics.Debug.WriteLine("BD respondio: " + SPResult);
            }


            // Se limpian variables para uso posterior de la funcion.
            NombreParametros.Clear();
            i = 0;
            System.Diagnostics.Debug.WriteLine("----- SP: " + querySP + ", ¡Ejecutado con exito! -----");
            return ResultFromSP;
        }

        public SqlXml ExecSPReturnSQLXML(string querySP, List<string> parametros)
        {
            System.Diagnostics.Debug.WriteLine("\n----- Intentado ejecutar SP: " + querySP + " -----");
            string SPResult = "";
            SqlXml xmlFromDatabase = new SqlXml();
            bool SPResultDetected = false;
            bool intDetected = false;

            int i = 0;
            int a = 0;
            int ResultFromSP = 0;
            // Esta lista contiene los nombres de los parametros de cualquier SP que mandemos llamar.
            List<string> NombreParametros = new List<string>();

            SqlCommand cmd = new SqlCommand(querySP, conn);
            cmd.CommandType = CommandType.StoredProcedure;

            conn.Open();
            // Obtiene los parametros del SP y llena la lista de cmd
            SqlCommandBuilder.DeriveParameters(cmd);

            foreach (SqlParameter p in cmd.Parameters)
            {
                // Nos saltamos el primer parametro por defecto
                if (i != 0)
                {
                    System.Diagnostics.Debug.WriteLine("Leyendo parametro: " + p.ParameterName);
                    NombreParametros.Add(p.ParameterName.ToString());
                }
                i++;
            }
            i = 0;
            cmd.Parameters.Clear();

            // Recorremos cada uno de los parametros que agregamos anteriormente para agregar los que
            // realmente necesitamos a cmd.Parameters.
            foreach (string p in NombreParametros)
            {
                if (p == "@mensaje")
                {
                    cmd.Parameters.Add(p, SqlDbType.VarChar, 150);
                    cmd.Parameters[p].Direction = ParameterDirection.Output;
                    SPResultDetected = true;
                    System.Diagnostics.Debug.WriteLine("Se agrega: " + p + ", " + "' '");
                }
                else
                {
                    // Detecta si el parametro que optuvimos puede ser considerado como int o string.
                    intDetected = int.TryParse(parametros[i], out a);
                    if (intDetected)
                    {
                        cmd.Parameters.AddWithValue(p.ToString(), a);
                        System.Diagnostics.Debug.WriteLine("Se agrega INT: " + p + ", " + parametros[i]);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue(p.ToString(), parametros[i]);
                        System.Diagnostics.Debug.WriteLine("Se agrega: " + p + ", " + parametros[i]);
                    }
                }

                i++;
            }

            // Agregamos un parametro OUTPUT para devolver un entero de SP's especiales.
            // Ojo: Necesitas tener un return(@ParameterReturnXML) en tu sp con un valor INT valido.
            var ParameterReturnXML = cmd.Parameters.Add("@ParameterReturnXML", SqlDbType.Xml);
            ParameterReturnXML.Direction = ParameterDirection.ReturnValue;

            cmd.ExecuteReader();
            conn.Close();


            // Guardamos el dato de los SP en una variable xml.
            xmlFromDatabase = (SqlXml)ParameterReturnXML.Value;

            // Si detecta un mensaje lo guarda y lo despliega en consola.
            if (SPResultDetected)
            {
                SPResult = cmd.Parameters["@mensaje"].Value.ToString();
                System.Diagnostics.Debug.WriteLine("BD respondio: " + SPResult);
            }


            // Se limpian variables para uso posterior de la funcion.
            NombreParametros.Clear();
            i = 0;
            System.Diagnostics.Debug.WriteLine("----- SP: " + querySP + ", ¡Ejecutado con exito! -----");
            return xmlFromDatabase;
        }

        //Obtiene todos los parametros del SP automaticamente y devuelve un DataTable
        public DataTable ExecSP(string querySP, List<string> parametros)
        {
            System.Diagnostics.Debug.WriteLine("\n----- Intentado ejecutar SP: " + querySP + " -----");
            DataTable dt = new DataTable();
            // SPResult es la variable que se encarga de guardar el mensaje resultante de un SP (StoredProcedure).
            string SPResult = "";
            bool SPResultDetected = false;
            bool intDetected = false;
            // i es la variable cuyo unico objetivo es ser un contador para la lista parametros que recibimos.
            int i = 0;
            int a = 0;
            // Esta lista contiene los nombres de los parametros de cualquier SP que mandemos llamar.
            List<string> NombreParametros = new List<string>();

            // Iniciamos la variable cmd indicandole que sera un SP con el nombre de la variable "querySP"
            // y utilizara la conexion de la variable "conexion".
            SqlCommand cmd = new SqlCommand(querySP, conn);
            cmd.CommandType = CommandType.StoredProcedure;

            // Se abre la conexion y se recopilan todos los parametros que se piden en el SP de la
            // variable "cmd". (ojo: este metodo te llena el espacio de los parametros de cmd.Parameters).
            conn.Open();
            SqlCommandBuilder.DeriveParameters(cmd);

            // El siguiente ciclo guarda los parametros recopilados en una variable tipo lista<string>
            // para posteriormente poder tener acceso al nombre de todos los parametros utilizados
            // en el SP que mandamos llamar.
            foreach (SqlParameter p in cmd.Parameters)
            {
                // Nos saltamos el primer parametro con la condicion "i != 0" ya que el primer parametro
                // es el resultado obligatorio de cada query que se ejecuta, y ese no debemos sobreescribirlo
                // ni nos interesa guardarlo.
                if (i != 0)
                {
                    System.Diagnostics.Debug.WriteLine("Leyendo parametro: " + p.ParameterName);
                    NombreParametros.Add(p.ParameterName.ToString());
                }
                i++;
            }
            i = 0;

            // Limpiamos los parametros obtenidos en nuestro primer metodo para tener el espacio libre.
            cmd.Parameters.Clear();

            // Recorremos cada uno de los parametros que agregamos anteriormente para agregar los que
            // realmente necesitamos a cmd.Parameters.
            foreach (string p in NombreParametros)
            {

                // Para los parametros OUTPUT en los SP, se maneja un estandar. Todos los parametros OUTPUT
                // se llamaran @mensaje en los SP y contendran un mensaje de tipo varchar(150).
                // Asi esta condicion detecta si el parametro leido es de tipo OUTPUT y ejecuta un codigo
                // especial para el.
                if (p == "@mensaje")
                {
                    cmd.Parameters.Add(p, SqlDbType.VarChar, 150);
                    cmd.Parameters[p].Direction = ParameterDirection.Output;
                    SPResultDetected = true;
                    System.Diagnostics.Debug.WriteLine("Se agrega: " + p + ", " + "' '");
                }
                else
                {

                    // Detecta si el parametro que optuvimos puede ser considerado como int o una string.
                    intDetected = int.TryParse(parametros[i], out a);
                    if (intDetected)
                    {
                        cmd.Parameters.AddWithValue(p.ToString(), a);
                        System.Diagnostics.Debug.WriteLine("Se agrega INT: " + p + ", " + parametros[i]);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue(p.ToString(), parametros[i]);
                        System.Diagnostics.Debug.WriteLine("Se agrega: " + p + ", " + parametros[i]);
                    }
                }

                i++;
            }

            // Ejecutamos el SP y cerramos la conexion.
            dt.Load(cmd.ExecuteReader());
            conn.Close();

            // Cualquier mensaje establecido en el parametro @mensaje de tipo OUTPUT se guardara en la variable
            // SPResult y se desplegara por default en la consola.
            if (SPResultDetected)
            {
                SPResult = cmd.Parameters["@mensaje"].Value.ToString();
                System.Diagnostics.Debug.WriteLine("BD respondio: " + SPResult);
            }


            // Se limpian variables para uso posterior de la funcion.
            NombreParametros.Clear();
            i = 0;
            System.Diagnostics.Debug.WriteLine("----- SP: " + querySP + ", ¡Ejecutado con exito! -----");
            return dt;
        }

        public List<string> ReturnAll_XML_FromUser(string rfc)
        {
            DataTable dt = new DataTable();
            List<string> listXML = new List<string>();
            List<string> attributes = new List<string>();
            attributes.Add(rfc);

            dt = ExecSP("ConsultaFactura", attributes);

            foreach (DataRow row in dt.Rows)
            {
                listXML.Add(row["xml"].ToString());
            }

            return listXML;
        }

        public string FormatPeriod(string date)
        {
            string Month = "";
            Month = date.Substring(5,2);
            Debug.WriteLine("MONTH ACORTADO: " + Month);

            switch (Month)
            {
                case "01":
                    {
                        Month = "Enero";
                        break;
                    }
                case "02":
                    {
                        Month = "Febrero";
                        break;
                    }
                case "03":
                    {
                        Month = "Marzo";
                        break;
                    }
                case "04":
                    {
                        Month = "Abril";
                        break;
                    }
                case "05":
                    {
                        Month = "Mayo";
                        break;
                    }
                case "06":
                    {
                        Month = "Junio";
                        break;
                    }
                case "07":
                    {
                        Month = "Julio";
                        break;
                    }
                case "08":
                    {
                        Month = "Agosto";
                        break;
                    }
                case "09":
                    {
                        Month = "Septiembre";
                        break;
                    }
                case "10":
                    {
                        Month = "Octubre";
                        break;
                    }
                case "11":
                    {
                        Month = "Noviembre";
                        break;
                    }
                case "12":
                    {
                        Month = "Diciembre";
                        break;
                    }
                default:
                    {
                        Month = "XXX";
                        break;
                    }
            }

            return Month;
        }

        public bool DeleteXML_Record(string InvoiceNumber)
        {
            bool result = false;

            SqlCommand cmd = new SqlCommand("[BajaFactura]");
            cmd.Connection = conn;
            cmd.CommandType = CommandType.StoredProcedure;


            cmd.Parameters.AddWithValue("@FolioFactura", InvoiceNumber);
            conn.Open();
            cmd.ExecuteNonQuery();
            conn.Close();
            result = true;

            return result;
        }
    }
}
