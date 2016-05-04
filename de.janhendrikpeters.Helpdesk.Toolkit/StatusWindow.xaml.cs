using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using LiveCharts;

namespace de.janhendrikpeters.helpdesk.toolkit
{
    /// <summary>
    /// Interaction logic for StatusWindow.xaml
    /// </summary>
    public partial class StatusWindow : Window
    {
        Dictionary<string, bool> _machineStatus = new Dictionary<string, bool>();
        private object _lock = new object();
        double _count = 0;
        public static System.Threading.CancellationTokenSource Cts = new System.Threading.CancellationTokenSource();
        ParallelOptions _po = new ParallelOptions();

        public StatusWindow()
        {
            InitializeComponent();

            //we create a new SeriesCollection
            Series = new SeriesCollection();

            _po.CancellationToken = Cts.Token;

            //create some series
            var availableSeries = new PieSeries
            {
                Title = Properties.Resources.String_AvailableMachines,
                Values = new ChartValues<double> { 0 },
                Fill = Brushes.Green
            };
            var unavailableSeries = new PieSeries
            {
                Title = Properties.Resources.Label_UnavailableMachines,
                Values = new ChartValues<double> { 0 },
                Fill = Brushes.Red
            };

            //add series to SeriesCollection
            Series.Add(availableSeries);
            Series.Add(unavailableSeries);

            //that's it, LiveCharts is ready and listening for your data changes.
            DataContext = this;

            System.Timers.Timer labelRefresh = new System.Timers.Timer();
            labelRefresh.Interval = 1000;
            labelRefresh.AutoReset = true;
            labelRefresh.Elapsed += LabelRefresh_Elapsed;

            _pingTimer.Interval = TimeSpan.FromMinutes(5).TotalMilliseconds;
            _count = _pingTimer.Interval;
            _pingTimer.AutoReset = true;
            _pingTimer.Elapsed += PingTimer_Elapsed;
            _pingTimer.Start();
            labelRefresh.Start();

            PingTimer_Elapsed(null, null);
        }

        private void LabelRefresh_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var tm = sender as System.Timers.Timer;

            _count -= 1000;

            if (_count == 0)
            {
                _count = _pingTimer.Interval;
            }

            Dispatcher.Invoke(new Action(() => TextBlockRefresh.Text = string.Format(Properties.Resources.String_Refresh, TimeSpan.FromMilliseconds(_count))));
        }

        private void PingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            foreach (string item in MainWindow.Machines)
            {
                if (_machineStatus.ContainsKey(item)) continue;

                _machineStatus.Add(item, false);
            }

            new Task(new Action(() =>
            {
                try
                {
                    Parallel.ForEach(_machineStatus.Keys.ToList(), _po, machine =>
                         {
                             if (Cts.IsCancellationRequested) return;

                             try
                             {
                                 var ping = new Ping();
                                 var result = ping.Send(machine);

                                 if (result.Status == IPStatus.Success)
                                 {
                                     lock (_lock)
                                     {
                                         _machineStatus[machine] = true;
                                     }
                                 }
                                 else
                                 {
                                     lock (_lock)
                                     {
                                         _machineStatus[machine] = false;
                                     }
                                 }
                                 if (Cts.IsCancellationRequested) return;
                             }
                             catch (Exception exc)
                             {
                                 Console.WriteLine(exc.Message);
                             }
                         }
                        );
                }
                catch (AggregateException aecx)
                {
                    Console.WriteLine("Many many errors");
                }

                try
                {
                    Dispatcher.Invoke(new Action(() =>
                    {

                        lock (_lock)
                        {
                            Series[0].Values[0] = (double)_machineStatus.Count(x => x.Value);
                        }
                        lock (_lock)
                        {
                            Series[1].Values[0] = (double)_machineStatus.Count(x => !x.Value);
                        }
                    }));
                }
                catch (Exception exc)
                {
                    Console.WriteLine(exc.Message);
                }

                Console.WriteLine("temp");
            })).Start();
        }

        public SeriesCollection Series { get; set; }

        private System.Timers.Timer _pingTimer = new System.Timers.Timer();

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Cts.Cancel();
        }
    }
}