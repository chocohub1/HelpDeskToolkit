using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using de.janhendrikpeters.helpdesk.library;

namespace de.janhendrikpeters.helpdesk.toolkit
{
    /// <summary>
    /// Interaction logic for ChangeServices.xaml
    /// </summary>
    public partial class ChangeServices : Window
    {
        public Dictionary<string, bool> MachineStatus;
        public ChangeServices()
        {
            InitializeComponent();
            MachineStatus = new Dictionary<string, bool>();
            DataGridServiceStatus.ItemsSource = MachineStatus;
        }

        private void ButtonStartService_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxServiceName.Text))
            {
                MessageBox.Show(Properties.Resources.String_EnterProcessBeforeKill);
                return;
            }

            ButtonStartService.IsEnabled = false;

            string serviceName = TextBoxServiceName.Text;
            List<string> machines = MachineStatus.Keys.ToList();
            ProgressBarCurrentOperation.Maximum = machines.Count;
            ProgressBarCurrentOperation.Value = 0;

            new Task(new Action(() =>
            {
                foreach (var machine in machines)
                {
                    MachineStatus[machine] = Helper.Instance.StartService(machine, serviceName, false);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        ProgressBarCurrentOperation.Value++;
                        DataGridServiceStatus.Items.Refresh();
                    }));
                }

                Dispatcher.Invoke(new Action(() => ButtonStartService.IsEnabled = true));
            })).Start();
        }

        private void ButtonRestartService_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxServiceName.Text))
            {
                MessageBox.Show(Properties.Resources.String_EnterProcessBeforeKill);
                return;
            }

            ButtonRestartService.IsEnabled = false;

            string serviceName = TextBoxServiceName.Text;
            List<string> machines = MachineStatus.Keys.ToList();
            ProgressBarCurrentOperation.Maximum = machines.Count;
            ProgressBarCurrentOperation.Value = 0;

            new Task(new Action(() =>
            {
                foreach (var machine in machines)
                {
                    MachineStatus[machine] = Helper.Instance.RestartService(machine, serviceName, false);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        ProgressBarCurrentOperation.Value++;
                        DataGridServiceStatus.Items.Refresh();
                    }));
                }

                Dispatcher.Invoke(new Action(() => ButtonRestartService.IsEnabled = true));
            })).Start();
        }

        private void ButtonStopService_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxServiceName.Text))
            {
                MessageBox.Show(Properties.Resources.String_EnterProcessBeforeKill);
                return;
            }

            ButtonStopService.IsEnabled = false;

            string serviceName = TextBoxServiceName.Text;
            List<string> machines = MachineStatus.Keys.ToList();
            ProgressBarCurrentOperation.Maximum = machines.Count;
            ProgressBarCurrentOperation.Value = 0;

            new Task(new Action(() =>
            {
                foreach (var machine in machines)
                {
                    MachineStatus[machine] = Helper.Instance.StopService(machine, serviceName, false);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        ProgressBarCurrentOperation.Value++;
                        DataGridServiceStatus.Items.Refresh();
                    }));
                }

                Dispatcher.Invoke(new Action(() => ButtonStopService.IsEnabled = true));
            })).Start();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DataGridServiceStatus_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.Column.Header.ToString() == "Key")
                e.Column.Header = Properties.Resources.String_Server;
            if (e.Column.Header.ToString() == "Value")
                e.Column.Header = Properties.Resources.String_Status;
        }
    }
}
