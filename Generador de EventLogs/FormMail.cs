using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Mail;

namespace Generador_de_EventLogs
{
    public partial class FormMail : Form
    {
        


        public FormMail()
        {
            InitializeComponent();
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            /*pasos del algoritmo:
             * 1.cargar eventos 
             * 2.ver     qué mes corresponde
             *3.filtrar los eventos correspondientes
              *4.escribir en el archivo
              *5.borrar eventos del archivo viejo
              *6.cambiar los numeros de los eventos que sobran
              *7.cambiar cantidad de eventos
              *8. enviar mail
              */

            try
            {
                #region cargar eventos en listaTotalEventos




                List<Evento> listaTotalEventos = Eventos.leerTodosLosEventos();
                progressBar1.Value = 1;
                #endregion

                #region calcular mes correspondiente y año
                string fecha = DateTime.Today.ToString();
                string[] separadores = new string[] { "/", " ", ":" };
                string mes = LeerCadena.leer(fecha, separadores, 1);
                int intMes = Convert.ToInt32(mes);
                intMes--;
                mes = "0" + intMes.ToString();
                string anio = LeerCadena.leer(fecha, separadores, 2);
                progressBar1.Value = 2;
                #endregion

                #region filtrar eventos y alojarlos en query
                IEnumerable<Evento> query = listaTotalEventos.Where(evento => evento.mes() == mes);
                progressBar1.Value = 3;
                #endregion

                #region escribir en el archivo
                string nombreArchivo = "EventLogs" + mes + "-" + anio + ".txt";

                if (File.Exists(nombreArchivo))
                {


                    DialogResult result = MessageBox.Show("Ya generaste anteriormente el log correspondiente al último mes, por favor borralo para generarlo de nuevo y poder enviar el mail. \t Quieres borrarlo?", "Ya se envió el e-mail anteriormente", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        File.Delete(nombreArchivo);
                        MessageBox.Show("El archivo fue borrado, pero el mail no se enviará.", "Borrado");
                        return;
                    }
                    else if (result == DialogResult.No)
                    {
                        return;
                    }



                }
                EventosAEnviar.cantEventos = 0;
                foreach (Evento evento in query)
                {
                    evento.guardarEvento(nombreArchivo);
                    EventosAEnviar.cantEventos++; //aumento en uno la cantidad de eventos filtrados/a enviar
                }

                progressBar1.Value++;
                #endregion


                #region Enviar Mail
                List<string> coordinadores = obtenerListaCoordinadores(); //obtengo lista de coordinadores
                string tipoMail = obtenerTipoMail(); //que servidor smtp voy a tener que usar?


                MailMessage email = new MailMessage();
                foreach (string coordinador in coordinadores)
                {
                    email.To.Add(new MailAddress(coordinador + "@imperiumao.com.ar"));
                }
                email.From = new MailAddress(textCorreo.Text);
                email.Subject = "Envío de Log Mensual";
                email.Body = "";
                email.IsBodyHtml = true;
                email.Priority = MailPriority.Normal;

                Attachment obj = new Attachment(nombreArchivo); //archivo adjunto 
                email.Attachments.Add(obj); //agrego el archivo a los adjuntos del mail

                SmtpClient smtp = new SmtpClient();

                switch (tipoMail)
                {
                    case "hotmail": smtp.Host = "smtp.live.com";
                        break;
                    case "imperiumao": smtp.Host = "smtp.gmail.com";
                        break;
                    case "gmail": smtp.Host = "smtp.gmail.com";
                        break;
                    case "yahoo": smtp.Host = "smtp.mail.yahoo.com";
                        break;
                    default: MessageBox.Show("El servidor de correo no se reconoce");
                        break;
                }

                smtp.Port = 25;
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(textCorreo.Text, textPass.Text);
                string output;
                try
                {
                    smtp.Send(email);
                    email.Dispose();
                    output = "Correo electronico enviado";
                }

                catch (Exception ex)
                {
                    output = "Error al enviar mail: " + ex.Message;
                }
                progressBar1.Value++;
                MessageBox.Show(output + " Ahora se borraran los eventos enviados del log...");




                #endregion


                #region borrar eventos enviados
                IEnumerable<Evento> query2 = listaTotalEventos.Where(evento => (!query.Contains(evento)));

                EventosAEnviar.cantEventos = 0;
                Eventos.modificarCantidadDeEventos(0);
                File.Delete("EventLogs.txt"); //borro todo el archivo de logs, los mismos estan cargados momentaneamente en memoria
                foreach (Evento evento in query2)
                {
                    evento.guardarEvento("EventLogs.txt");
                    EventosAEnviar.cantEventos++;
                }

                #endregion

            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

    
        public List<string> obtenerListaCoordinadores() //devuelve una coleccion de todos los coordinadores
        {
            string[] separadores = new string[] {"=","|" };
          
            try
            {
                StreamReader arch = new StreamReader("init.txt");
                string linea = arch.ReadLine();
                while (linea != "[Coordinadores]" & arch.Peek() > -1) // peek devuelve  el proximo caracter a leer, si no existe devuelve -1
                {
                    linea = arch.ReadLine();
                }

                linea = arch.ReadLine();
                string[] arrayStrings = linea.Split(separadores, StringSplitOptions.RemoveEmptyEntries);
                arch.Close();
                List<string> nuevaLista = new List<string>();
                for (int i = 1; i < arrayStrings.Length; i++)
                {
                    nuevaLista.Add(arrayStrings[i]);
                                       
                }
                
                return nuevaLista;

            }

            catch (Exception ex)
            {
                MessageBox.Show("Hubo un error: " + ex.Message);
                return new List<string>();
            }


        }


        public string obtenerTipoMail()
        {
            try
            {
                string mail = textCorreo.Text;
                string[] separadores = new string[] { "@", "." };
                string[] arrayStrings = mail.Split(separadores, StringSplitOptions.RemoveEmptyEntries);
                return arrayStrings[1];
            }

            catch (Exception)
            {
                return "";
            }
        }


    }
}
