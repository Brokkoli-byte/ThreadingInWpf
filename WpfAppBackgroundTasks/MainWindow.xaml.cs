using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
using WpfAppBackgroundTasks.Annotations;

namespace WpfAppBackgroundTasks
{
    /// <summary>
    /// Fenster zur Demonstrierung der Vorteile durch Threadhandling und der Unterschiede verschiedener Ansätze.
    /// <remarks>
    ///Verwendet werden <see cref="Task"/> und <see cref="BackgroundWorker"/>.
    /// </remarks>
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {

        #region Konstruktor

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
        }

        #endregion

        
        #region Properties

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public int _anzahlZahlen = 100;

        public int AnzahlZahlen
        {
            get => _anzahlZahlen;
            set
            {
                if (_anzahlZahlen == value) return;
                _anzahlZahlen = value;
                OnPropertyChanged();
            }
        }

        #endregion

        
        #region Eventhadler

        private void LetzteFibonacciZahl(object sender, RoutedEventArgs e)
        {
            if (rB_Mode_InDiesemThread.IsChecked ?? false)
                doIt_InDiesemThread();
            else if (rB_Mode_TaskFactory.IsChecked ?? false)
                doIt_TaskFactoryStyle();
            else if (rB_Mode_awaitTask.IsChecked ?? false)
                doIt_awaitTaskStyle();
            else if (rB_Mode_BackgroundWorker.IsChecked ?? false)
                doIt_BackgroundworkerStyle();
        }

        #endregion


        #region Methoden für die verschiedenen Threadingmodi

        /// <summary>
        /// Berechnet die x.te Fibonacci-Zahl im aktuellen Thread.
        /// Wird genutzt um zu demsonstrieren, dass sich der UI-Thread blockiert wird wenn von den
        /// .NET Threading Tools kein Gebrauch gemacht wird.
        /// </summary>
        void doIt_InDiesemThread() => setAnzeige(getFib());

        /// <summary>
        /// Soll die x.te Fibonaccizahl mithilfe von Task.Factory berechnen.
        /// Funktioniert noch nicht!
        /// </summary>
        
        [Obsolete]
         void doIt_TaskFactoryStyle()
        {
            var calc = Task.Factory.StartNew(() => calcTask).Unwrap();
            setAnzeige(calc.Result);
        }

        /// <summary>
        /// Führt die Berechnung der x.ten Fibonacci-Zahl in einem seperaten Task durch.
        /// setAnzeige erst auf, wenn der Task beendet ist.
        /// Ist eine Asynchrone Methode, weil in ihr auf den Abschluss der Berechnung gewartet wird.
        /// </summary>
        async void doIt_awaitTaskStyle()
        {
            var calc = calcTask;
            calc.Start();
            await calc; // Wartet, bis der Task beendet ist. So wird erst nach beenden setAnzeige aufgerufen.
            setAnzeige(calc.Result);
        }

        /// <summary>
        /// Führt die Berechnung in einem seperaten Thread mithilfe eines Instanz von <see cref="BackgroundWorker"/> durch.
        /// Zudem wird ein Hilfsfenster mit einer ProgressBar angezeigt.
        /// Die Instanz von <see cref="BackgroundWorker"/> leitet den Fortschritt an die ProgressBar weiter, welche diesen anzeigt.
        /// </summary>
        void doIt_BackgroundworkerStyle()
        {
            BackgroundWorker bw = new BackgroundWorker { WorkerReportsProgress = true };
            var calcWin = new calcWindow();

            #region Eventhandler des Backgroundworkers

            bw.DoWork += (o, e) =>
             {
                 int anzahl = (int)e.Argument;
                 long vorletzteZahl = 0, letzteZahl = 1;
                 int percentageReached = anzahl / 100;
                 for (var step = 0; step < anzahl; step++)
                 {
                     var cache = letzteZahl;
                     letzteZahl += vorletzteZahl;
                     vorletzteZahl = cache;
                     if (step % percentageReached == 0) bw.ReportProgress(step / (anzahl / 100));
                 }

                 e.Result = letzteZahl;
             };
            bw.ProgressChanged += (o, e) => calcWin.progBar.Value = e.ProgressPercentage;
            bw.RunWorkerCompleted += (o, e) => setAnzeige((long)e.Result);
            bw.RunWorkerCompleted += (o, e) => calcWin.Close();

            #endregion

            calcWin.Show();
            bw.RunWorkerAsync(AnzahlZahlen);

        }

        #endregion

        
        #region Hilfsmethoden
        
        public void setAnzeige(long erg) => Anzeige.Text = erg.ToString();

        private Task<long> calcTask => new Task<long>(getFib);

        long getFib()
        {
            long vorletzteZahl = 0;
            long letzteZahl = 1;
            long cache;
            int zuErmittelndeZahlen = AnzahlZahlen;
            for (int step = 0; step < zuErmittelndeZahlen; step++)
            {
                cache = letzteZahl;
                letzteZahl += vorletzteZahl;
                vorletzteZahl = cache;
            }

            return letzteZahl;
        }
        #endregion
    }
}
