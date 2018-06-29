using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace LSTABINT_SERV
{
    public partial class Service1 : ServiceBase
    {
        private System.Timers.Timer tmGenera = null;
        private int i;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            tmGenera = new System.Timers.Timer();
            tmGenera.Interval = 10000;
            tmGenera.Elapsed += TmGenera_Elapsed;
            tmGenera.Enabled = true;
            tmGenera.Start();
        }
        public void Inicio()
        {
            tmGenera = new System.Timers.Timer();
            tmGenera.Interval = 10000;
            tmGenera.Elapsed += TmGenera_Elapsed;
            tmGenera.Enabled = true;
            tmGenera.Start();
        }

        private void TmGenera_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            tmGenera.Enabled = false;
            
            GeneraArchivo();
        }

        private void GeneraArchivo()
        {
            i++;
            variables varparametros = new variables();
            varparametros = parametros(varparametros);
            archivonormal(varparametros);
            encabezados(varparametros);
            EventLog.WriteEntry("Se ha creado un archivo LSTBINT con la extensión" + i.ToString());
            
            tmGenera.Enabled = true;
        }

        protected override void OnStop()
            
        {
            EventLog.WriteEntry("oh oh, algo ha ocurrido mal :(");
        }

        public variables parametros(variables nuevasvar)
        {
            string query = "Data Source=.;Initial Catalog=GuatemalaTelepeaje;Integrated Security=True";
            var conexion = new SqlConnection();
            conexion.ConnectionString = query;
            conexion.Open();
            string consulta = "select * from Parametrizable";
            string consulta2 = "update Parametrizable set ListbindEXT = ListbindEXT + 1";
            var cmd2 = new SqlCommand(consulta2, conexion);
            cmd2.ExecuteNonQuery();
            var cmd = new SqlCommand(consulta, conexion);
            var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            if (dt.Rows.Count > 0)
            {
                nuevasvar.Origen = Convert.ToString(dt.Rows[0][1].ToString());
                nuevasvar.Destino = Convert.ToString(dt.Rows[0][0].ToString());
                nuevasvar.extension = Convert.ToString(dt.Rows[0][7].ToString());
                nuevasvar.fmt = Convert.ToString(dt.Rows[0][2].ToString());
                nuevasvar.fmtResidente = Convert.ToString(dt.Rows[0][3]);
                nuevasvar.OrigenResidentes = Convert.ToString(dt.Rows[0][8]);
                nuevasvar.DestinoResidentes = Convert.ToString(dt.Rows[0][9]);
                nuevasvar.DestinoAntifraudes = Convert.ToString(dt.Rows[0][11]);
                nuevasvar.DestinoMontominimo = Convert.ToString(dt.Rows[0][12]);
                nuevasvar.cruzes = Convert.ToInt16(dt.Rows[0][5]);
                nuevasvar.minutos = Convert.ToInt16(dt.Rows[0][6]);
                char[] prueba = nuevasvar.extension.ToCharArray();
                if (prueba.Length > 3)
                {
                    nuevasvar.extension = nuevasvar.extension.Substring((Convert.ToInt32(prueba.Length) - 3), 3);
                }
                nuevasvar.extension = nuevasvar.extension.PadLeft(3, '0');
                if (nuevasvar.extension == "000")
                {
                    string query3 = "update Parametrizable set ListbindEXT = ListbindEXT + 1";
                    var cm3 = new SqlCommand(query3, conexion);
                    cm3.ExecuteNonQuery();
                    nuevasvar.extension = "001";
                }
                nuevasvar.Origen.Replace(@"\\", @"\");
                nuevasvar.Destino.Replace(@"\\", @"\");
            }
            nuevasvar.VDestino = nuevasvar.Destino + nuevasvar.extension;
            conexion.Close();

            return nuevasvar;
        }
        public variables encabezados(variables varenca)
        {
            bool exists = System.IO.Directory.Exists(@"C:\temporal\");
            if (!exists)
            {
                System.IO.Directory.CreateDirectory(@"C:\temporal\");
            }
            varenca.VOrigen = @"C:\temporal\" + "LSTABINT.";
            var aplicationdate = DateTime.Now.ToString("yyyyMMddHHmm");
            var creationdate = DateTime.Now.ToString("yyyyMMddHHmm");
            //var objReader = new StreamReader(varenca.VOrigen + varenca.extension);

            string formato = "000000";

            string[] lines = System.IO.File.ReadAllLines(varenca.VOrigen + varenca.extension);
            string countlines = lines.LongLength.ToString(formato);
            countlines = countlines.Substring(countlines.Length - 6, 6);

            countlines = (Convert.ToInt32(countlines) - 1).ToString();
            countlines = countlines.PadLeft(6, '0');
            //objReader.Close();


            string encabezados = "63" + aplicationdate + creationdate + "0100" + varenca.extension + countlines + "\n";

            //File.AppendAllText(varenca.VOrigen + varenca.extension, lines[0]);
            //System.IO.File.WriteAllLines(varenca.VOrigen + varenca.extension, lines);
            //File.AppendAllText(varenca.VOrigen + varenca.extension, encabezados);
            File.Delete(varenca.VOrigen + varenca.extension);
            File.AppendAllText(varenca.VOrigen + varenca.extension, encabezados);
            File.AppendAllLines(varenca.VOrigen + varenca.extension, lines);
            return varenca;
        }
        public void archivonormal(variables variableslistas)
        {

            string query = "Data Source=.;Initial Catalog=GuatemalaTelepeaje;Integrated Security=True";
            var conexion = new SqlConnection();
            conexion.ConnectionString = query;
            conexion.Open();
            bool exists = System.IO.Directory.Exists(@"C:\temporal\");
            if (!exists)
            {
                System.IO.Directory.CreateDirectory(@"C:\temporal\");
            }
            string final = "\\0'";
            string consulta;
            consulta = "Exec master ..xp_Cmdshell 'bcp " + "\"select NoTag + replicate (space(1), 24- LEN(NoTag)) + (''0''+TipoTag + IIF(EstatusTag = 1,''01'',''00'')+  REPLICATE(''0'', 8 - LEN(SaldoTags.Saldo))+CONVERT(nvarchar,Saldo)+SUBSTRING(NoTag,0,14)+REPLICATE(space(1), 19 - LEN(SUBSTRING(NoTag,0,14))) + IIF(EstatusResidente = 1,''01'',''00'') + REPLICATE(''0'', 49)) from GuatemalaTelepeaje.dbo.Tags JOIN GuatemalaTelepeaje.dbo.SaldoTags ON SaldoTags.Id = Tags.SaldoTagId\" queryout \"" + variableslistas.VDestino + "\"" + " -T -c -t" + final;
            var cmd = new SqlCommand(consulta, conexion);
            cmd.CommandTimeout = 3 * 60;
            cmd.ExecuteNonQuery();
        }
    }
}
