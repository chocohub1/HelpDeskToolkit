using de.janhendrikpeters.helpdesk.library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace de.janhendrikpeters.helpdesk.toolkit
{
    /// <summary>
    /// Interaction logic for KillProcess.xaml
    /// </summary>
    public partial class KillProcess : Window
    {
        public Dictionary<string, bool> MachineStatus;
        public string ProcessName;
        public KillProcess()
        {
            InitializeComponent();
            MachineStatus = new Dictionary<string, bool>();
            DataGridKillResults.ItemsSource = MachineStatus;
        }

        private void ButtonKill_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxProcessName.Text))
            {
                MessageBox.Show(Properties.Resources.String_EnterProcessBeforeKill);
                return;
            }

            if (string.IsNullOrWhiteSpace(System.IO.Path.GetExtension(TextBoxProcessName.Text)))
            {
                TextBoxProcessName.Text = System.IO.Path.ChangeExtension(TextBoxProcessName.Text, "exe");
            }

            ButtonKill.IsEnabled = false;

            string processName = TextBoxProcessName.Text;
            List<string> machines = MachineStatus.Keys.ToList();
            ProgressBarCurrentOperation.Maximum = machines.Count;
            ProgressBarCurrentOperation.Value = 0;
            
            new Task(new Action(() =>
            {
                object localLockObject = new object();

                Parallel.ForEach<string, KeyValuePair<string, bool>>(
                        machines,
                        () => { return new KeyValuePair<string, bool>(); },
                        (machine, state, localKeyValuePair) =>
                        {
                            localKeyValuePair = new KeyValuePair<string, bool>(machine, Helper.Instance.KillProcessByName(processName, machine));

                            Dispatcher.Invoke(new Action(() =>
                            {
                                ProgressBarCurrentOperation.Value++;
                            }));
                            return localKeyValuePair;
                        },
                        (finalResult) =>
                        {
                            lock (localLockObject) MachineStatus[finalResult.Key] = finalResult.Value;
                        }
                );

            })).Start();
        }

        private void DataGridKillResults_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.Column.Header.ToString() == "Key")
                e.Column.Header = Properties.Resources.String_Server;
            if (e.Column.Header.ToString() == "Value")
                e.Column.Header = Properties.Resources.String_Status;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
