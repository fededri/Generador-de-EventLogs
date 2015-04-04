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
using System.Diagnostics;
using System.Net;
using System.Net.Mail;




/*Changelog:
 * La carga de eventos se realiza desde el archivo lista eventos.txt, se pueden agregar o sacar los eventos que se quieran,
 * si el archivo no existe crea uno por default con algunos eventos predeterminados
 * Boton para abrir/cerrar IAO, la direccion se guarda en init
 * Calculador de porcentajes de premios
 * */


/*ToDO: 
 * Manejar los cambios en los textos  y el comboBox
 * Crear form con estadísticas, cantidad de eventos por mes, mayor evento realizado etc
 *  
 * */


namespace Generador_de_EventLogs
{
        
    public partial class Form1 : Form
    {
        static Evento nuevoEvento = new Evento();
        static Boolean copioLogInicial, copioLogFinal;

        public struct distribucion{
           public double totalPrimerPuesto;
           public double totalSegundoPuesto;
           public double totalTercerPuesto;
           public double individualPrimerPuesto;
           public double individualSegundoPuesto;
           public double individualTercerPuesto;
           public int inscripcion;
        }

        public string obtenerDireccion() //obtiene la direccion de la carpeta de IAO, si no existe devuelve cadena vacia
        {
            string[] separadores = new string[] { "=" };
            StreamReader arch = new StreamReader("init.txt");
            string linea = arch.ReadLine();
            try
            {
                while (linea != "[DireccionIAO]" & arch.Peek() > -1) // peek devuelve  el proximo caracter a leer, si no existe devuelve -1
                {
                    linea = arch.ReadLine();
                }
                linea = arch.ReadLine();
                string[] listaStrings = linea.Split(separadores, StringSplitOptions.RemoveEmptyEntries);
                arch.Close();
                return listaStrings[1];
            }

            catch (NullReferenceException) //capturo el error de no lectura de dirrecion
            {
                arch.Close();
                return "";
            }

        }


        private void cargarEventosBasicosDefault(StreamWriter arch) // carga eventos hardcodeados y los guarda en el archivo, si el archivo no existe
        {
            string[] eventos = new string[] {"Torneo 1vs1", "Torneo 2vs2", "Torneo 3vs3",
                                             "Punteria","Trivia","Artes Marciales","Plantado",
                                             "Juego De La Silla", "Vale Todo", "DeathMatch",
                                             "Rey De Pista","Laberinto","Supervivencia","Campeador",
                                             "Ataca al Peón","Invasión","Guerra","Captura la gema"};
            int cant = eventos.Length;
            for (int i = 0; i < cant; i++)
            {
                comboEventos.Items.Add(eventos[i]);
                arch.WriteLine(eventos[i]);
            }

        }

        private void cargarEventosEnComboBox()
        {
            string path = "lista eventos.txt";
            if (!File.Exists(path))
            {
                StreamWriter arch = File.CreateText(path);

                cargarEventosBasicosDefault(arch);
                arch.Close();

            }
            else
            {
                //***********************
                //Carga los eventos al comboBox desde el archivo de eventos si ya existe

                TextReader archivoEventos = new StreamReader(path);
                string linea = archivoEventos.ReadLine();

                while (linea != null)
                {
                    comboEventos.Items.Add(linea);
                    linea = archivoEventos.ReadLine();

                }
                archivoEventos.Close();
                //************************
            }



        }

        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            DialogResult result = MessageBox.Show("Seguro que quieres salir? Toda información del evento en curso se perderá", "Saliendo", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                Application.Exit();
            }
            else
            {
                e.Cancel = true;
            }
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                cargarEventosEnComboBox(); // acá ya se revisa si existe el archivo lista eventos
                Evento nuevoEvento = new Evento();

                if (!File.Exists("init.txt"))
                {
                    throw new FileNotFoundException(); //lanzo excepcion
                }
                actualizarLabel();
               
                
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("No se pudo encontrar init, creando...");
                StreamWriter arch = File.CreateText("init.txt");
                arch.WriteLine("[CantEventos]");
                arch.WriteLine("Cantidad=0");
                arch.WriteLine();
                arch.WriteLine("[DireccionIAO]");
                arch.WriteLine(@"Direccion=C:\Archivos De Programa\ImperiumAO 1.5");
                arch.WriteLine();
                arch.WriteLine("[Coordinadores]");
                arch.WriteLine("Coordinadores=");
                arch.Close();
            }


        }


        private void fixCantEventos()//Esta funcion corrige diferencias en la cantidad de eventos
        {
            int res = chequearCantEventos();
            if(res != -1) // si el checkeo dio negativo
            {
                Eventos.modificarCantidadDeEventos(res);                
                actualizarLabel();
            }

        }

        private int chequearCantEventos() //Esta funcion verifica que la cantidad de eventos en el INIT sea la real
        {
            int cantidadReal = Eventos.calcularCantidadDeEventosReal();
            return cantidadReal == Eventos.cantidadDeEventos() ? -1 : cantidadReal;
           
        }

        
        private void actualizarLabel()
        {
            labelCant.Text = Eventos.cantidadDeEventos().ToString()+ " eventos registrados";
        }

        private void buttonInicial_Click(object sender, EventArgs e)
        {
            try
            {
                if(nuevoEvento.guardado)
                {
                    MessageBox.Show("Este evento ya fue guardado", "Abortando");
                    return;
                }
                //Asigno las variables del evento
                string nombre = comboEventos.SelectedItem.ToString();
                string dms = textDms.Text;
                string mapa = textMapa.Text;
                string cantParticipantes = textParticipantes.Text;
                string inscripcion = textInscripcion.Text;
                string comentario = textBoxComentario.Text;
                string niveles = textNiveles.Text;
                //***********************************

                string cadena; //string a guardar el /LOG
               nuevoEvento = new Evento(nombre, dms, mapa, cantParticipantes, inscripcion, comentario,niveles);
               cadena = nuevoEvento.obtenerLogInicial();
               Clipboard.SetText(cadena);
               copioLogInicial = true;
               textInfo.Text = "El siguiente texto fue copiado al portapapeles: " + Environment.NewLine + cadena;
            }

            catch (NullReferenceException)
            {
                MessageBox.Show("El ComboBox tiene un objeto nulo");
            }
        }


        private void buttonSalir_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void buttonFinal_Click(object sender, EventArgs e)
        {
            //GENERACIÓN DEL /LOG FINAL
            if (Form1.copioLogInicial)
            {
                nuevoEvento.ganadores = textGanadores.Text;
                nuevoEvento.premios = textPremio.Text;
                nuevoEvento.SetHoraFinal();

                string cadena = nuevoEvento.obtenerLogFinal();
                Clipboard.SetText(cadena);
                Form1.copioLogFinal = true;
               
                DialogResult result = MessageBox.Show("Se ha guardado el siguiente texto en el portapapeles: " + cadena + "\t Queres guardar el evento?", "Finalizando evento", MessageBoxButtons.YesNo);
              
                if (result == DialogResult.Yes)
               {
                   if (nuevoEvento.guardado)
                   {
                       MessageBox.Show("El evento ya fue guardado antes", "Operación Abortada");
                       return;

                   }
                   else
                   {
                       nuevoEvento.guardarEvento("EventLogs.txt");
                   }
                   
                   

               }
            }
            else
            {
                MessageBox.Show("No se puede generar rem final sin primero generar el inicial");
            }
            //***************************************
        }

        private void buttonGuardar_Click(object sender, EventArgs e)
        {
            if (!copioLogFinal)
            {
                MessageBox.Show("Primero debe copiar el log final e inicial para poder guardar el evento");
                return;
            }
            if (nuevoEvento.guardado)
            {
                MessageBox.Show("El evento ya fue guardado antes", "Operación Abortada");
                return;

            }
            else
            {
                nuevoEvento.guardarEvento("EventLogs.txt");
            }
            

        }


        private void button1_Click(object sender, EventArgs e) //boton para abrir IAO
        {
            


        }

        private void button2_Click(object sender, EventArgs e)
        {


        }

        private void buttonEmail_Click(object sender, EventArgs e)
        {
            FormMail frm = new FormMail();
            frm.Show();
        }

        private void buttonLimpiar_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Se resetearan todas las variables, estás seguro?", "Borrar de memoria Evento actual", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                limpiarTodo();
                nuevoEvento = new Evento();
                copioLogFinal = false;
                copioLogInicial = false;               
            }
            
        }

        public void limpiarTodo()
        {
            foreach (Control c in this.Controls)
            {
                if (c is TextBox)
                {
                    c.Text = "";
                }
                else if (c is ComboBox)
                {
                    c.Text = "";
                }

            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            Eventos.leerTodosLosEventos();
           

        }

        private void buttonRegistros_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("EventLogs.txt");
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void abrirIAO_OnClick(object sender, ToolStripItemClickedEventArgs e)
    {
        MessageBox.Show("ASD");
    }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Programa creado por Drihtion para uso del staff de Imperium AO","About");
        }

        private void abrirIAOToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (abrirIAOToolStripMenuItem.Text == "Abrir IAO")
            {
                try
                {
                    ProcessStartInfo info = new ProcessStartInfo();
                    info.UseShellExecute = true;
                    info.FileName = "ImperiumAOLauncher.exe";
                    info.WorkingDirectory = obtenerDireccion();
                    Process.Start(info);
                    abrirIAOToolStripMenuItem.Text = "Cerrar IAO";

                }

                catch (Win32Exception ex)
                {
                    MessageBox.Show(ex.Message + " (Modificá la ruta desde el archivo INIT)");
                }
            }
            else
            {
                try
                {
                    Process[] myProcesses = Process.GetProcessesByName("ImperiumAO");
                    foreach (Process myProcess in myProcesses)
                    {
                        myProcess.CloseMainWindow();
                    }

                }
                catch (IndexOutOfRangeException ex)
                {
                    MessageBox.Show(ex.Message);
                }

                abrirIAOToolStripMenuItem.Text = "Abrir IAO";
            }
        }

        private void deA1ToolStripMenuItem_Click(object sender, EventArgs e) //70 30 8 individual
        {
                int inscripcion = Convert.ToInt32(textInscripcion.Text);
                calcularPorcentajesDeDos(1, 8, 0.7, 0.3);
                
                  

        }

        private distribucion calcularPorcentajesDeDos(int cantXgrupo, int cantGrupos, double porcentajePrimero, double porcentajeSegundo)
        {
            distribucion distribucion = new distribucion();
            try
            {
                distribucion.inscripcion = Convert.ToInt32(textInscripcion.Text);
                
                int total = distribucion.inscripcion * cantGrupos;
                distribucion.totalPrimerPuesto = total * porcentajePrimero;
                distribucion.individualPrimerPuesto = distribucion.totalPrimerPuesto / cantXgrupo;

                distribucion.totalSegundoPuesto = total * porcentajeSegundo;
                distribucion.individualSegundoPuesto = distribucion.totalSegundoPuesto / cantXgrupo;

                mostrarPorcentajesDeDos(distribucion);    //imprimo los datos                   


                return distribucion;
            }
            catch (FormatException)
            {
                textInfo.Text = "Hay un error en el formato ingresado en la inscripción";
                return new distribucion();
            }

        }
        private void  mostrarPorcentajesDeDos(distribucion distribucion)
        {
             textInfo.Text = "Total Primer Puesto: " + Environment.NewLine + distribucion.totalPrimerPuesto + Environment.NewLine +
                                "Individual Primer Puesto: " + Environment.NewLine + distribucion.individualPrimerPuesto + Environment.NewLine +
                                "Total Segundo Puesto: " + Environment.NewLine + distribucion.totalSegundoPuesto + Environment.NewLine +
                                "Individual Segundo Puesto: " + Environment.NewLine + distribucion.individualSegundoPuesto + Environment.NewLine;
        }

        private void mostrarPorcentajesDeTres(distribucion distribucion)
        {
            mostrarPorcentajesDeDos(distribucion);
            textInfo.Text = textInfo.Text + "Total Tercer Puesto" + Environment.NewLine + distribucion.totalTercerPuesto + Environment.NewLine +
                            "Individual Tercer Puesto: " + Environment.NewLine + distribucion.individualTercerPuesto;
 
        }

        private distribucion calcularPorcentajesDeTres(int cantXgrupo, int cantGrupos, double porcentajePrimero, double porcentajeSegundo,
                                                       double porcentajeTercero)
        {
            distribucion distribucion = calcularPorcentajesDeDos(cantXgrupo, cantGrupos, porcentajePrimero, porcentajeSegundo);
            int total = distribucion.inscripcion * cantGrupos;

            distribucion.totalTercerPuesto = total * porcentajeTercero;
            distribucion.individualTercerPuesto = distribucion.totalTercerPuesto / cantXgrupo;
            mostrarPorcentajesDeTres(distribucion);    //imprimo los datos  
            return distribucion;

        }

        private void deA1ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeTres(1, 8, 0.7, 0.2, 0.1);

        }

        private void button1_Click_2(object sender, EventArgs e)
        {
           
        }

        private void button1_Click_3(object sender, EventArgs e)
        {
            string horaCadena = "01:15:33";
            DateTime horaInicio = Convert.ToDateTime(horaCadena);
            MessageBox.Show(horaInicio.ToString());

        }

        private void deA2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeDos(2, 8, 0.7, 0.3);
        }

        private void deA3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeDos(3, 8, 0.7, 0.3);
        }

        private void deAUnoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeDos(1, 16, 0.7, 0.3);
        }

        private void deADosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeDos(2, 16, 0.7, 0.3);
        }

        private void deATresToolStripMenuItem_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeDos(3, 16, 0.7, 0.3);
        }

        private void deAUnoToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeDos(1, 32, 0.7, 0.3);
        }

        private void deADosToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeDos(2, 32, 0.7, 0.3);
        }

        private void deATresToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeDos(3, 32, 0.7, 0.3);
        }

        private void deA1ToolStripMenuItem4_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeDos(1, 8, 0.8, 0.2);
        }

        private void deA2ToolStripMenuItem4_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeDos(2, 8, 0.8, 0.2);
        }

        private void deA3ToolStripMenuItem4_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeDos(3, 8, 0.8, 0.2);
        }

        private void deA1ToolStripMenuItem5_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeDos(1, 16, 0.8, 0.2);
        }

        private void deA2ToolStripMenuItem5_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeDos(2, 16, 0.8, 0.2);
        }

        private void deA3ToolStripMenuItem5_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeDos(3, 16, 0.8, 0.2);
        }

        private void toolStripMenuItem10_Click(object sender, EventArgs e)
        {

        }

        private void deA1ToolStripMenuItem6_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeDos(1, 32, 0.8, 0.2);
        }

        private void deA2ToolStripMenuItem6_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeDos(2, 32, 0.8, 0.2);
        }

        private void deA3ToolStripMenuItem6_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeDos(3, 32, 0.8, 0.2);
        }

        private void deA2ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeTres(2, 8, 0.7, 0.2, 0.1);
        }

        private void deA3ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeTres(3, 8, 0.7, 0.2, 0.1);
        }

        private void deA1ToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeTres(1, 16, 0.7, 0.2, 0.1);
        }

        private void deA2ToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeTres(2, 16, 0.7, 0.2, 0.1);
        }

        private void deA3ToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeTres(3, 16, 0.7, 0.2, 0.1);
        }

        private void deA1ToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeTres(1, 32, 0.7, 0.2, 0.1);
        }

        private void deA2ToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeTres(2, 32, 0.7, 0.2, 0.1);
        }

        private void deA3ToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            calcularPorcentajesDeTres(3, 32, 0.7, 0.2, 0.1);
        }

        private void cantidadDeEventosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fixCantEventos();
        }

        private void abrirInitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("init.txt");
        }

        
       
        
    }

    public class Evento
    {
        protected string nombreEvento,dmsColaboradores, premiosDados, mapaUsado, cuantosParticiparon, precioInscripcion, nombreGanadores, comment, levels;
        public bool guardado;
        public DateTime horaInicio, horaFin, fecha;


        public string niveles
        {
            get
            {
                return levels;
            }
            set
            {
                levels = value;
            }
        }
        public string nombre {

            get
            {
                return nombreEvento;
            }

            set
            {
                nombreEvento = value;
            }


    }

        public string dms
        {
            get
            {
                return dmsColaboradores;
            }

            set
            {
                dmsColaboradores = value;
            }

        }

        public string mapa{

            get {
                return mapaUsado;
            }

            set {
                mapaUsado = value;
            }
        }

        public string cantParticipantes{
            get{
                return cuantosParticiparon;
            }
            set {
                cuantosParticiparon = value;
            }
         }


        public string inscripcion{

            get {
                return precioInscripcion;
            }
            set {
                precioInscripcion = value;
            }
        }

        public string premios
        {
            get
            {
                return premiosDados;
            }

            set
            {
                premiosDados = value;
            }

        }

        public string ganadores
        {
            get
            {
                return nombreGanadores;
            }

            set
            {
                nombreGanadores = value;
            }

        }


        public string comentario
        {
            get
            {
                return comment;
            }
            set
            {
                comment = value;
            }
        }

        public Evento(string nombre, string dms, string mapa, string cantParticipantes, string inscripcion, string comentario, string niveles)
        {

            this.nombre = nombre;
            this.dms = dms;
            this.mapa = mapa;
            this.cantParticipantes = cantParticipantes;
            this.inscripcion = inscripcion;
            this.SetHoraInicial();
            this.comentario = comentario;
            this.fecha = DateTime.Today;
            this.niveles = niveles;
        }


        public Evento()
        {
            
            
        }

     


        public DateTime SetHoraInicial()
        {

            this.horaInicio = DateTime.Now;
            return this.horaInicio;
        }

            public string obtenerLogInicial() // sin precio de inscripcion
        {
            string cadena=null;
            cadena = "/LOG Comienza " + this.nombre + " Dms a cargo: " + this.dms + " Mapa: " + this.mapa + " Cantidad de participantes: " + this.cantParticipantes+" Niveles: "+this.niveles;
            if (this.inscripcion != "")
            {
                cadena = cadena+" Inscripción: " + this.inscripcion;
            }
            if (this.comentario != "")
            {
                cadena = cadena+" Comentario: "+ this.comentario;
            }
            cadena=cadena+"****************************************************";
            
                return cadena;
        }

        public string obtenerLogFinal()
        {
            string cadena;
            cadena = "/LOG Finaliza " + this.nombre + " Ganadores: " + this.ganadores + " Premios: " + this.premios + "****************************************************";
            return cadena;
        }

        public bool seInicializo()
        {
            return this.nombre != null;
        }

        public void SetHoraFinal()
        {
            this.horaFin = DateTime.Now;
        }

          public string obtenerHora(string cadena)
          {
              string[] separadores = new string[] {" "};

              string hora = LeerCadena.leer(cadena, separadores, 1);
                  return hora;
           
          }

          public string obtenerFecha(string fecha)
          {
              string[] separadores = new string[] {" "};
              string[] arrayStrings = fecha.Split(separadores,StringSplitOptions.RemoveEmptyEntries);
              return arrayStrings[0];
          }

        public void guardarEvento(string file)
          {
              // GUARDADO DEL EVENTO EN EVENTLOGS.TXT

              StreamWriter archivo = File.AppendText(file);
              int cantEventos;

              #region que valor debe tener cantEventos
              if (file == "EventLogs.txt")
              {
                  cantEventos = Eventos.cantidadDeEventos();
              }
              else
              {
                  cantEventos = EventosAEnviar.cantEventos;
              }
              #endregion    


              cantEventos++;                
              
              archivo.WriteLine("[EventNum" + cantEventos + "]");
              archivo.WriteLine("Tipo de Evento: " + this.nombre);
              archivo.WriteLine("Ganadores: " + this.ganadores);
              archivo.WriteLine("Premios: " + this.premios);
              archivo.WriteLine("Dms a cargo: " + this.dms);
              archivo.WriteLine("Mapa: " + this.mapa);
              archivo.WriteLine("Fecha: " + this.obtenerFecha(this.fecha.ToString()));
              archivo.WriteLine("Comenzó a las " + this.obtenerHora(this.horaInicio.ToString()));
              archivo.WriteLine("Finalizó a las " + this.obtenerHora(this.horaFin.ToString()));
              archivo.WriteLine("Participantes: " + this.cantParticipantes);
              archivo.WriteLine("Niveles: " + this.niveles);
              if (this.inscripcion != "")
              {
                  archivo.WriteLine("Precio de inscripción: " + this.inscripcion);
              }
              if (this.comentario != "")
              {
                  archivo.WriteLine("Comentario: " + this.comentario);
              }
              archivo.WriteLine("*****************************");
              if (file == "EventLogs.txt")
              {
                  Eventos.incrementarCantidad();
              }
              this.guardado = true;
              archivo.Close();
              //*************************
          } 


        public string mes()
        {
            string cadenaFecha = this.obtenerFecha(this.fecha.ToString());
            string[] separadores = new string[] { "/" };
            return LeerCadena.leer(cadenaFecha, separadores, 1);

        }


    }

    
    public class Eventos
    {
        protected List<Evento> eventos;

        public List<Evento> listaEventos
        {
            get
            {
                return eventos;
            }
            set
            {
                eventos = value;
            }

        }
        

        public Eventos() // el constructor genera una lista de todos los eventos guardados en el archivo (lista de objetos tipo evento) 
        {
            if (File.Exists("EventLogs.txt"))
            { listaEventos = leerTodosLosEventos(); }
        }

        static public int calcularCantidadDeEventosReal()
        {
            StreamReader arch = new StreamReader("EventLogs.txt");
            
            int contador=0;
            string linea;
            while(!arch.EndOfStream)
            {
                linea = arch.ReadLine();
                if (linea == "*****************************")
                {
                    contador++;
                }               

            }
            return contador;
        }

        static public int cantidadDeEventos() //retorna -1 si no llega a leer la cantidad de eventos
        {
            StreamReader arch = new StreamReader("init.txt");
            
            string linea = null;
            while (linea != "[CantEventos]")
            {
                linea = arch.ReadLine();
            }

            if (linea == "[CantEventos]")
            {
                linea = arch.ReadLine();
                string[] separadores = new string[] { "=" };
                                
                arch.Close();
                return Convert.ToInt32(LeerCadena.leer(linea,separadores,1));
            }
            else
            {
                MessageBox.Show("Hubo un problema con la cantidad de eventos del archivo init");
                return -1;
            }


        }

        static public void modificarCantidadDeEventos(int numero)
        {
            string init = leerInit(); // lee TODO el archivo
            StreamReader arch = new StreamReader("init.txt");
            string linea = arch.ReadLine();

            while (linea != "[CantEventos]")
            {
                linea = arch.ReadLine();
            }
            linea = arch.ReadLine();
            string lineaVieja = linea; //guardo la linea vieja para despues poder reemplazarla

            string[] separadores = new string[] { "=" };
            string cadenaDeCantidad = LeerCadena.leer(linea, separadores, 1);
                       
            linea = linea.Replace(cadenaDeCantidad, numero.ToString()); // linea a colocar en el archivo

            init = init.Replace(lineaVieja, linea);
            arch.Close();

            StreamWriter archivo = new StreamWriter("init.txt");
            archivo.Write(init);
            archivo.Close();

        }

        static public List<Evento> leerTodosLosEventos()
        {
            try
            {
                StreamReader arch = new StreamReader("EventLogs.txt");
                List<Evento> eventos = new List<Evento>();
                int cantidad = cantidadDeEventos();
                Evento unEvento = new Evento();

                for (int i = 1; i <= cantidad; i++)
                {
                    unEvento = leerSiguienteEvento(arch);
                    eventos.Add(unEvento);
                   
                }
                arch.Close();
                return eventos;
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return new List<Evento>();
            }
        }

        static public Evento leerSiguienteEvento(StreamReader arch)
        {
            Evento evento = new Evento();
            string linea = arch.ReadLine();
            string[] separadores = new string[] { ":"};
            string[] separadorDeHora = new string[] {" ","p","a"};
            string[] arrayStrings = new string[] { };

            
            linea = arch.ReadLine(); // lee el tipo de evento                      
            evento.nombre = LeerCadena.leer(linea, separadores, 1);

            linea = arch.ReadLine(); //lee los ganadores
           
            evento.ganadores = LeerCadena.leer(linea, separadores, 1);

            linea = arch.ReadLine(); //premios
           // arrayStrings = linea.Split(separadores, StringSplitOptions.RemoveEmptyEntries);
            evento.premios = LeerCadena.leer(linea, separadores, 1).Replace(" ", "");

            linea = arch.ReadLine(); //dms
            //arrayStrings = linea.Split(separadores, StringSplitOptions.RemoveEmptyEntries);
            evento.dms = LeerCadena.leer(linea, separadores, 1).Replace(" ", "");

            linea = arch.ReadLine(); //mapa
            //arrayStrings = linea.Split(separadores, StringSplitOptions.RemoveEmptyEntries);
            evento.mapa = LeerCadena.leer(linea, separadores, 1).Replace(" ", "").Replace(" ", "");

            linea = arch.ReadLine(); //fecha
            arrayStrings = linea.Split(separadores, StringSplitOptions.RemoveEmptyEntries);            
            evento.fecha = Convert.ToDateTime(arrayStrings[1].Replace(" ", ""));

            #region HORARIOS DE INICIO Y FIN (UNA PAJA)

            linea = arch.ReadLine(); //hora inicio
            arrayStrings = linea.Split(separadorDeHora, StringSplitOptions.RemoveEmptyEntries);            
            evento.horaInicio = Convert.ToDateTime(arrayStrings[3]);
            

            linea = arch.ReadLine(); //hora fin
            arrayStrings = linea.Split(separadorDeHora, StringSplitOptions.RemoveEmptyEntries);
            evento.horaFin = Convert.ToDateTime(arrayStrings[4]); // nota: como separadorDeHora tiene la 'a' separa en Fin li s etc


            #endregion


            linea = arch.ReadLine(); //participantes
            evento.cantParticipantes = LeerCadena.leer(linea, separadores, 1).Replace(" ", "");

            linea = arch.ReadLine(); //niveles
            evento.niveles = LeerCadena.leer(linea, separadores, 1).Replace(" ", "");

            linea = arch.ReadLine(); //inscripcion [OPCIONAL]
            arrayStrings = linea.Split(separadores, StringSplitOptions.RemoveEmptyEntries);
            if (arrayStrings[0] == "Precio de inscripción")
            {
                evento.inscripcion = LeerCadena.leer(linea, separadores, 1).Replace(" ", "");
                linea = arch.ReadLine(); // ahora veo si le sigue un comentario
                arrayStrings = linea.Split(separadores, StringSplitOptions.RemoveEmptyEntries);
                if (arrayStrings[0] == "Comentario")
                {
                    evento.comentario = LeerCadena.leer(linea, separadores, 1).Replace(" ", "");
                    arch.ReadLine();// leo la linea formada por ***************
                }
                else
                {
                    return evento;
                }

            }
            else if (arrayStrings[0] == "Comentario")
            {
                evento.comentario = LeerCadena.leer(linea, separadores, 1).Replace(" ", "");
                arch.ReadLine();// leo la linea formada por ***************

            }
            

         


            

            return evento;


        }

        static public int incrementarCantidad()
        {
            /*El algoritmo consiste en primero leer todo el archivo init y guardarlo en una variable, luego leer la linea de cantidad de eventos creando
             * dos variables (una para después encontrar en donde hay que reemplazar de INIT), luego leer el valor siguiente al =, pasarlo a int, incrementarlo 
             * en uno, pasarlo a string y reemplazar en la linea leida el nuevo valor. Luego se reemplaza la linea vieja por la ultima en la cadena init
             * Finalmente se guarda todo el contenido de la cadena init en el archivo sobreescribiendo lo que haya */
            string init = leerInit();
            StreamReader arch = new StreamReader("init.txt");
            string linea = arch.ReadLine();
          
            while (linea != "[CantEventos]")
            {
                linea = arch.ReadLine();
            }
            linea= arch.ReadLine();
            string lineaVieja = linea; //guardo la linea vieja para despues poder reemplazarla

            string[] separadores = new string[] { "=" };
            string cadenaDeCantidad = LeerCadena.leer(linea, separadores, 1);

            int cantidadEventos = Convert.ToInt32(cadenaDeCantidad);
            cantidadEventos++;
            linea = linea.Replace(cadenaDeCantidad, cantidadEventos.ToString()); // linea a colocar en el archivo
            
           init= init.Replace(lineaVieja, linea);
            arch.Close();
                        
            StreamWriter archivo = new StreamWriter("init.txt");
            archivo.Write(init);
            archivo.Close();
            return cantidadEventos;

        }


       static public string leerInit()
        {
            StreamReader arch = new StreamReader("init.txt");
            string init= arch.ReadToEnd();
            arch.Close();
                return init;
        }


    }


    public class LeerCadena
    {

        // devuelve el string en la posicion numero del array de strings obtenido como resultado al aplicar Split sobre cadena usando los separadores
       static  public string leer(string cadena, string[] separadores, int numero)
        {
            string[] arrayStrings = cadena.Split(separadores, StringSplitOptions.RemoveEmptyEntries);
            return arrayStrings[numero];

        }


    }

    public class EventosAEnviar : Eventos
    {
      public static int cantEventos; 



    }

}






