using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using de.janhendrikpeters.helpdesk.library;

namespace de.janhendrikpeters.helpdesk.toolkit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static ObservableCollection<string> Machines { get; set; }
        public static ObservableCollection<EventEntryInfo> EventEntries = new ObservableCollection<EventEntryInfo>();
        System.Threading.CancellationTokenSource _tokenSource = new System.Threading.CancellationTokenSource();
        System.Threading.CancellationTokenSource _serverRefreshSource = new System.Threading.CancellationTokenSource();
        System.Threading.CancellationToken _serverRefreshToken = new System.Threading.CancellationToken();
        private LogConsole _lc = new LogConsole();
        private Dictionary<string, Dictionary<string, List<string>>> _domainDivisionMachineDictionary = new Dictionary<string, Dictionary<string, List<string>>>();
        TreeViewItem _newItem = new TreeViewItem();
        TreeViewItem _divItem = new TreeViewItem();
        TreeViewItem _machItem = new TreeViewItem();
        ListView _machineListView = new ListView();
        List<ListView> _machineListViews = new List<ListView>();
        string _selectedServer = null;

        public MainWindow()
        {

            InitializeComponent();
            _serverRefreshToken = _tokenSource.Token;
            Machines = new ObservableCollection<string>();
            LabelStatusInfo.Content = Properties.Resources.LabelStatusInfo_ContentStandard;
            TabControlMain.IsEnabled = false;

            Application.Current.DispatcherUnhandledException += CurrentDomain_UnhandledException;

            ComboBoxLanguage.Items.Add("en");
            ComboBoxLanguage.Items.Add("de");
            new Task(RefreshServerList, _serverRefreshToken).Start();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CurrentDomain_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            StackTrace stackTrace = new StackTrace(e.Exception, true);
            StackFrame[] frames = stackTrace.GetFrames();
            string text = string.Empty;
            bool flag = frames != null;
            if (flag)
            {
                StackFrame[] array = frames;
                for (int i = 0; i < array.Length; i++)
                {
                    StackFrame stackFrame = array[i];
                    bool flag2 = stackFrame.GetFileName() == null;
                    if (flag2) continue;
                    try
                    {
                        text += string.Format(Properties.Resources.String_ExceptionSource, stackFrame.GetFileName());
                        text += string.Format(Properties.Resources.String_ExceptionMethod, stackFrame.GetMethod().Name);
                        text += string.Format(Properties.Resources.String_ExceptionOccurrence, stackFrame.GetFileLineNumber(), stackFrame.GetFileColumnNumber());
                    }
                    catch
                    {
                    }
                }
            }
            e.Handled = true;
            string text2 = string.Format(Properties.Resources.String_ErrorMessageBox, e.Exception.Message + ((e.Exception.InnerException != null) ? ("\n" + e.Exception.InnerException.Message) : null));


            text2 += text;
            bool flag3 = MessageBox.Show(text2, Properties.Resources.String_AppError, MessageBoxButton.YesNo, MessageBoxImage.Hand) == MessageBoxResult.No;
            if (flag3)
            {
                _tokenSource.Cancel();
                Application.Current.Shutdown();
            }
        }

        private void ShowAboutWindow(object sender, RoutedEventArgs e)
        {
            var compiledate = System.Reflection.Assembly.GetExecutingAssembly().GetLinkerTime();
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            MessageBox.Show(string.Format(Properties.Resources.String_AboutText, version, compiledate), Properties.Resources.String_AboutCaption, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowStatusWindow(object sender, RoutedEventArgs e)
        {
            StatusWindow sw = new StatusWindow();
            sw.Show();
        }

        private void ListViewMachines_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListView lv = sender as ListView;
            if (lv == null) return;

            _serverRefreshSource.Cancel();
            _serverRefreshSource = new CancellationTokenSource();

            if (!(lv.HasItems && lv.SelectedItems.Count == 1)) return;


            TabControlMain.IsEnabled = true;
            _selectedServer = lv.SelectedItem.ToString();
            var token = _serverRefreshSource.Token;

            new Task(new Action(() =>
            {
                // Pending reboot
                RefreshLabel(LabelStatusInfo, string.Format(Properties.Resources.String_RetrieveRebootStatus, _selectedServer), Brushes.Red);

                if (token.IsCancellationRequested) return;
                try
                {
                    bool result = Helper.Instance.IsRebootPending(_selectedServer);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        string temp;
                        if (result)
                        {
                            temp = string.Format(Properties.Resources.LabelRebootStatusPending, _selectedServer);
                            RefreshLabel(LabelRebootStatus, temp, Brushes.Red);
                        }
                        else
                        {
                            temp = string.Format(Properties.Resources.LabelRebootStatusOk, _selectedServer);
                            RefreshLabel(LabelRebootStatus, temp, Brushes.Green);
                        }
                    }));
                }
                catch (System.Security.SecurityException exc)
                {
                    MessageBox.Show(string.Format(Properties.Resources.Error_InsufficientPermissions, Properties.Resources.Action_RebootStatus, exc.Message));
                }
                catch (UnauthorizedAccessException exc)
                {
                    MessageBox.Show(string.Format(Properties.Resources.Error_InsufficientPermissions, Properties.Resources.Action_RebootStatus, exc.Message));
                }
                catch (Exception exc)
                {
                    MessageBox.Show(string.Format(Properties.Resources.Error_GenericError, Properties.Resources.Action_RebootStatus, exc.Message));
                }

                // Processes
                if (token.IsCancellationRequested) return;
                RefreshLabel(LabelStatusInfo, string.Format(Properties.Resources.String_RetrieveProcesses, _selectedServer), Brushes.Red);
                try
                {
                    var proc = Helper.Instance.GetProcesses(_selectedServer);
                    Dispatcher.Invoke(new Action(() => { DataGridProcesses.ItemsSource = proc; }));
                }
                catch (System.ComponentModel.Win32Exception exc)
                {
                    MessageBox.Show(string.Format(Properties.Resources.Error_InsufficientPermissions, Properties.Resources.Action_Processes, exc.Message));
                }
                catch (Exception exc)
                {
                    MessageBox.Show(string.Format(Properties.Resources.Error_GenericError, Properties.Resources.Action_Processes, exc.Message));
                }

                // Services
                if (token.IsCancellationRequested) return;
                RefreshLabel(LabelStatusInfo, string.Format(Properties.Resources.String_RetrieveServices, _selectedServer), Brushes.Red);
                try
                {
                    var svc = Helper.Instance.GetServices(_selectedServer);
                    Dispatcher.Invoke(new Action(() => { DataGridServices.ItemsSource = svc; }));
                }
                catch (System.ComponentModel.Win32Exception exc)
                {
                    MessageBox.Show(string.Format(Properties.Resources.Error_InsufficientPermissions, Properties.Resources.Action_Services, exc.Message));
                }
                catch (Exception exc)
                {
                    MessageBox.Show(string.Format(Properties.Resources.Error_GenericError, Properties.Resources.Action_Services, exc.Message));
                }

                // Tasks
                if (token.IsCancellationRequested) return;
                RefreshLabel(LabelStatusInfo, string.Format(Properties.Resources.String_RetrieveTasks, _selectedServer), Brushes.Red);
                try
                {
                    var tasks = Helper.Instance.GetScheduledTasks(_selectedServer);
                    Dispatcher.Invoke(new Action(() => { DataGridTasks.ItemsSource = tasks; }));
                }
                catch (Exception exc)
                {
                    MessageBox.Show(string.Format(Properties.Resources.Error_GenericError, Properties.Resources.Action_Tasks, exc.Message));
                }

                // Software
                if (token.IsCancellationRequested) return;
                RefreshLabel(LabelStatusInfo, string.Format(Properties.Resources.String_RetrieveInstalledSoftware, _selectedServer), Brushes.Red);
                try
                {
                    var software = Helper.Instance.GetInstalledSoftware(_selectedServer);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        DataGridSoftware.ItemsSource = software;
                    }));
                }
                catch (System.Security.SecurityException exc)
                {
                    MessageBox.Show(string.Format(Properties.Resources.Error_InsufficientPermissions, Properties.Resources.Action_Software, exc.Message));
                }
                catch (UnauthorizedAccessException exc)
                {
                    MessageBox.Show(string.Format(Properties.Resources.Error_InsufficientPermissions, Properties.Resources.Action_Software, exc.Message));
                }
                catch (Exception exc)
                {
                    MessageBox.Show(string.Format(Properties.Resources.Error_GenericError, Properties.Resources.Action_Software, exc.Message));
                }

                // Event log
                if (token.IsCancellationRequested) return;
                RefreshLabel(LabelStatusInfo, string.Format(Properties.Resources.String_RetrieveEventLogs, _selectedServer), Brushes.Red);
                try
                {
                    var logs = Helper.Instance.GetEventLogs(_selectedServer);
                    Dispatcher.Invoke(new Action(() => { ListViewEventLogs.ItemsSource = logs; }));
                }
                catch (System.Security.SecurityException exc)
                {
                    MessageBox.Show(string.Format(Properties.Resources.Error_InsufficientPermissions, Properties.Resources.Action_EventLogs, exc.Message));
                }
                catch (UnauthorizedAccessException exc)
                {
                    MessageBox.Show(string.Format(Properties.Resources.Error_InsufficientPermissions, Properties.Resources.Action_EventLogs, exc.Message));
                }
                catch (Exception exc)
                {
                    MessageBox.Show(string.Format(Properties.Resources.Error_GenericError, Properties.Resources.Action_EventLogs, exc.Message));
                }

                RefreshLabel(LabelStatusInfo, Properties.Resources.LabelStatusInfo_ContentStandard, Brushes.Green);
            }), token).Start();
        }

        private void ButtonReloadServerList_Click(object sender, RoutedEventArgs e)
        {
            Machines.Clear();
            new Task(RefreshServerList, _tokenSource.Token).Start();
        }

        private void RefreshServerList()
        {
            Forest currentForest = Forest.GetCurrentForest();
            DomainCollection domains = currentForest.Domains;
            string domainRegex = System.Configuration.ConfigurationManager.AppSettings["DomainRegexFilter"];
            string orgUnitRegex = System.Configuration.ConfigurationManager.AppSettings["OrganizationalUnitRegexFilter"];

            foreach (Domain objDomain in domains)
            {
                if (_serverRefreshToken.IsCancellationRequested) return;
                if (!Regex.IsMatch(objDomain.Name, domainRegex, RegexOptions.IgnoreCase)) continue;


                Stopwatch sw = new Stopwatch();
                sw.Start();

                // Add domain to dictionary
                _domainDivisionMachineDictionary.Add(objDomain.Name, new Dictionary<string, List<string>>());

                Dispatcher.Invoke(new Action(() =>
                {
                    _newItem = new TreeViewItem();
                    _newItem.Header = objDomain.Name;
                    TreeViewMachines.Items.Add(_newItem);
                }));

                using (DirectoryEntry sBase = new DirectoryEntry($"LDAP://{objDomain.Name}"))
                {
                    if (_serverRefreshToken.IsCancellationRequested) return;
                    using (DirectorySearcher ds = new DirectorySearcher(sBase))
                    {
                        string dn = sBase.Properties["distinguishedName"].Value.ToString();
                        string groupReplacement = sBase.Name.Substring(sBase.Name.Length - 3, 3);

                        ds.SearchScope = SearchScope.Subtree;
                        ds.Filter = "objectClass=organizationalUnit";

                        try
                        {
                            using (SearchResultCollection results = ds.FindAll())
                            {
                                foreach (SearchResult result in results)
                                {
                                    if (_serverRefreshToken.IsCancellationRequested) return;
                                    if (!Regex.IsMatch(result.GetDirectoryEntry().Name, orgUnitRegex, RegexOptions.IgnoreCase)) continue;

                                    // Add division (parent of current OU) to nested dictionary
                                    string parentOuName = string.Empty;
                                    try
                                    {
                                        parentOuName = result.GetDirectoryEntry().GetParentOrgUnit("Division");
                                        if (!_domainDivisionMachineDictionary[objDomain.Name].ContainsKey(parentOuName))
                                        {
                                            _domainDivisionMachineDictionary[objDomain.Name].Add(parentOuName, new List<string>());
                                        }


                                        Dispatcher.Invoke(new Action(() =>
                                        {
                                            _divItem = new TreeViewItem();
                                            _divItem.Header = parentOuName;
                                            _newItem.Items.Add(_divItem);

                                            _machineListView = new ListView();
                                            _machineListView.MouseDoubleClick += ListViewMachines_MouseDoubleClick;
                                            _divItem.Items.Add(_machineListView);
                                            _machineListViews.Add(_machineListView);
                                        }));
                                    }
                                    catch (Exception exc)
                                    {
                                        Console.WriteLine("uh oh\r\n" + exc.Message);
                                        continue;
                                    }

                                    try
                                    {
                                        string[] props = { "cn", "Enabled" };
                                        using (DirectorySearcher subDs = new DirectorySearcher(result.GetDirectoryEntry(), "(objectCategory=Computer)", props))
                                        {
                                            subDs.SearchScope = SearchScope.Subtree;

                                            using (SearchResultCollection subResults = subDs.FindAll())
                                            {
                                                var ide = subResults.GetEnumerator();
                                                ide.Reset();
                                                while (ide.MoveNext())
                                                {
                                                    SearchResult res = ide.Current as SearchResult;
                                                    if (_serverRefreshToken.IsCancellationRequested) return;
                                                    string name = res.Properties["cn"][0].ToString();

                                                    // Add all servers to division node
                                                    if (!_domainDivisionMachineDictionary[objDomain.Name][parentOuName].Contains(name))
                                                    {
                                                        _domainDivisionMachineDictionary[objDomain.Name][parentOuName].Add(name);
                                                    }


                                                    Dispatcher.Invoke(new Action(() =>
                                                    {
                                                        _machineListView.Items.Add(name);
                                                    }));

                                                    if (Machines.Contains(name)) continue;
                                                    Dispatcher.Invoke(new Action(() => Machines.Add(name)));
                                                }

                                                Dispatcher.Invoke(new Action(() => _machineListView.Items.SortDescriptions.Add(
                                                new System.ComponentModel.SortDescription("",
                                                System.ComponentModel.ListSortDirection.Ascending))));
                                            }
                                        }
                                    }
                                    catch (Exception exc)
                                    {
                                        Console.WriteLine(Properties.Resources.Error_StoreSearch + exc.Message);
                                    }
                                }
                            }
                        }
                        catch (Exception exc)
                        {
                            Console.WriteLine(Properties.Resources.Error_OUSearch + exc.Message);
                        }
                    }
                }

                sw.Stop();
                Dispatcher.Invoke(new Action(() =>
                {
                    _lc.TextBlockMessages.Text += $"{objDomain.Name}: {sw.Elapsed}\r\n";
                    _lc.ScrollViewerMessages.ScrollToEnd();
                }));
            }
        }

        private void ButtonStartQuery_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _tokenSource.Cancel();
            Application.Current.Shutdown();
        }

        private void ComboBoxLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //App.ChangeCulture(new CultureInfo(ComboBoxLanguage.SelectedItem.ToString()));

            MessageBox.Show(Properties.Resources.String_FunctionNotImplemented);
        }

        private void ButtonKillProcess_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridProcesses.SelectedItems == null) return;
            ((Button)sender).IsEnabled = false;

            List<int> pids = new List<int>();
            foreach (ProcessInformation item in DataGridProcesses.SelectedItems)
            {
                pids.Add(item.ProcessId);
            }

            new Task(new Action(() =>
            {
                RefreshLabel(LabelStatusInfo, string.Format(Properties.Resources.String_KillingProcesses, _selectedServer), Brushes.Red);

                try
                {
                    Helper.Instance.KillProcesses(pids, _selectedServer);
                }
                catch (Exception exc)
                {
                    MessageBox.Show(string.Format(Properties.Resources.Error_GenericError, Properties.Resources.Action_Processes, exc.Message));
                }

                var proc = Helper.Instance.GetProcesses(_selectedServer);
                Dispatcher.Invoke(new Action(() => { DataGridProcesses.ItemsSource = proc; }));

                Dispatcher.Invoke(new Action(() =>
                {
                    RefreshLabel(LabelStatusInfo, string.Format(Properties.Resources.String_AllProcessesKilled), Brushes.Green);
                    ((Button)sender).IsEnabled = true;
                }));
            })).Start();
        }

        private void LabelRebootStatus_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Helper.Instance.CurrentPendingReboot != null)
            {
                PendingRebootInfo pi = new PendingRebootInfo();
                pi.Show();
            }
        }

        private void ButtonStartService_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridServices.SelectedItems == null) return;
            ((Button)sender).IsEnabled = false;

            var services = new List<ServiceInformation>();
            foreach (ServiceInformation si in DataGridServices.SelectedItems) services.Add(si);

            new Task(new Action(() =>
            {
                try
                {
                    foreach (ServiceInformation si in services)
                    {
                        Dispatcher.Invoke(new Action(() => RefreshLabel(LabelStatusInfo, string.Format(Properties.Resources.String_StartingService, si.ServiceName), Brushes.Red)));
                        Helper.Instance.StartService(_selectedServer, si.ServiceName, true);
                    }

                    var svc = Helper.Instance.GetServices(_selectedServer);
                    Dispatcher.Invoke(new Action(() => { DataGridServices.ItemsSource = svc; }));
                }
                catch (System.ComponentModel.Win32Exception exc)
                {
                    MessageBox.Show(string.Format(Properties.Resources.Error_InsufficientPermissions, Properties.Resources.Action_Services, exc.Message));
                }
                catch (Exception exc)
                {
                    MessageBox.Show(string.Format(Properties.Resources.Error_GenericError, Properties.Resources.Action_Services, exc.Message));
                }

                Dispatcher.Invoke(new Action(() =>
                {
                    RefreshLabel(LabelStatusInfo, string.Format(Properties.Resources.String_AllServicesStarted), Brushes.Green);
                    ((Button)sender).IsEnabled = true;
                }));
            })).Start();
        }

        private void ButtonStopService_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridServices.SelectedItems == null) return;
            ((Button)sender).IsEnabled = false;

            var services = new List<ServiceInformation>();
            foreach (ServiceInformation si in DataGridServices.SelectedItems) services.Add(si);

            new Task(new Action(() =>
            {
                try
                {
                    foreach (ServiceInformation si in services)
                    {
                        Dispatcher.Invoke(new Action(() => LabelStatusInfo.Content = string.Format(Properties.Resources.String_StoppingService, si.ServiceName)));
                        Helper.Instance.StopService(_selectedServer, si.ServiceName, true);
                    }

                    var svc = Helper.Instance.GetServices(_selectedServer);
                    Dispatcher.Invoke(new Action(() => { DataGridServices.ItemsSource = svc; }));
                }
                catch (System.ComponentModel.Win32Exception exc)
                {
                    MessageBox.Show(string.Format(Properties.Resources.Error_InsufficientPermissions, Properties.Resources.Action_Services, exc.Message));
                }
                catch (Exception exc)
                {
                    MessageBox.Show(string.Format(Properties.Resources.Error_GenericError, Properties.Resources.Action_Services, exc.Message));
                }

                Dispatcher.Invoke(new Action(() =>
                {
                    LabelStatusInfo.Content = string.Format(Properties.Resources.String_AllServicesStopped);
                    ((Button)sender).IsEnabled = true;
                }));
            })).Start();
        }

        private void ButtonRestartService_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridServices.SelectedItems == null) return;
            ((Button)sender).IsEnabled = false;

            var services = new List<ServiceInformation>();
            foreach (ServiceInformation si in DataGridServices.SelectedItems) services.Add(si);


            new Task(new Action(() =>
            {
                try
                {
                    foreach (ServiceInformation si in services)
                    {
                        RefreshLabel(LabelStatusInfo, string.Format(Properties.Resources.String_RestartingService, si.ServiceName), Brushes.Red);

                        Helper.Instance.RestartService(_selectedServer, si.ServiceName, true);
                    }

                    var svc = Helper.Instance.GetServices(_selectedServer);
                    Dispatcher.Invoke(new Action(() => { DataGridServices.ItemsSource = svc; }));
                }
                catch (System.ComponentModel.Win32Exception exc)
                {
                    MessageBox.Show(string.Format(Properties.Resources.Error_InsufficientPermissions, Properties.Resources.Action_Services, exc.Message));
                }
                catch (Exception exc)
                {
                    MessageBox.Show(string.Format(Properties.Resources.Error_GenericError, Properties.Resources.Action_Services, exc.Message));
                }

                RefreshLabel(LabelStatusInfo, string.Format(Properties.Resources.String_AllServicesRestarted), Brushes.Green);

                Dispatcher.Invoke(new Action(() =>
                {
                    ((Button)sender).IsEnabled = true;
                }));
            })).Start();
        }

        private void LabelStatusInfo_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!_lc.IsVisible)
            {
                _lc.Show();
            }

            if (!_lc.IsFocused)
            {
                _lc.Focus();
            }
        }

        private void RefreshLabel(Label inputLabel, string labelText, Brush color)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                inputLabel.Content = labelText;
                inputLabel.Foreground = color;
                _lc.TextBlockMessages.Text += labelText + "\r\n";
                _lc.ScrollViewerMessages.ScrollToEnd();
            }));
        }

        private void ButtonStartTask_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridTasks.SelectedItems == null) return;

            ((Button)sender).IsEnabled = false;

            var taskList = new List<TaskInformation>();
            foreach (TaskInformation t in DataGridTasks.SelectedItems) taskList.Add(t);

            new Task(new Action(() =>
            {
                RefreshLabel(LabelStatusInfo, string.Format(Properties.Resources.String_StartingTasks, DataGridTasks.SelectedItems.Count), Brushes.Red);

                foreach (TaskInformation item in taskList)
                {
                    try
                    {
                        Helper.Instance.StartTask(null, item.TaskName);
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show(string.Format(Properties.Resources.String_TaskStartError, item.TaskName, exc.Message), Properties.Resources.Generic_Error);
                    }
                }

                var tasks = Helper.Instance.GetScheduledTasks(_selectedServer);
                Dispatcher.Invoke(new Action(() => { DataGridTasks.ItemsSource = tasks; }));

                RefreshLabel(LabelStatusInfo, string.Format(Properties.Resources.String_TasksStarted, DataGridTasks.SelectedItems.Count),
                    Brushes.Green);

                Dispatcher.Invoke(new Action(() => ((Button)sender).IsEnabled = true));
            })).Start();
        }

        private void ButtonStopTask_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridTasks.SelectedItems == null) return;

            ((Button)sender).IsEnabled = false;
            var taskList = new List<TaskInformation>();
            foreach (TaskInformation t in DataGridTasks.SelectedItems) taskList.Add(t);

            new Task(new Action(() =>
            {
                RefreshLabel(LabelStatusInfo, string.Format(Properties.Resources.String_StoppingTasks, DataGridTasks.SelectedItems.Count),
                    Brushes.Red);
                foreach (TaskInformation item in taskList)
                {
                    try
                    {
                        Helper.Instance.StopTask(null, item.TaskName);
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show(
                            string.Format(Properties.Resources.String_TaskStopError, item.TaskName, exc.Message),
                            Properties.Resources.Generic_Error);
                    }
                }

                var tasks = Helper.Instance.GetScheduledTasks(_selectedServer);
                Dispatcher.Invoke(new Action(() => { DataGridTasks.ItemsSource = tasks; }));

                RefreshLabel(LabelStatusInfo, string.Format(Properties.Resources.String_TasksStopped, DataGridTasks.SelectedItems.Count),
                    Brushes.Green);
                Dispatcher.Invoke(new Action(() => ((Button)sender).IsEnabled = true));
            })).Start();
        }

        private void ButtonEnableTask_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridTasks.SelectedItems == null) return;

            ((Button)sender).IsEnabled = false;

            var taskList = new List<TaskInformation>();
            foreach (TaskInformation t in DataGridTasks.SelectedItems) taskList.Add(t);

            new Task(new Action(() =>
            {
                RefreshLabel(LabelStatusInfo, string.Format(Properties.Resources.String_EnablingTasks, DataGridTasks.SelectedItems.Count),
                    Brushes.Red);
                foreach (TaskInformation item in taskList)
                {
                    try
                    {
                        Helper.Instance.EnableTask(null, item.TaskName);
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show(
                            string.Format(Properties.Resources.String_ErrorEnablingTasks, item.TaskName, exc.Message),
                            Properties.Resources.Generic_Error);
                    }
                }

                var tasks = Helper.Instance.GetScheduledTasks(_selectedServer);
                Dispatcher.Invoke(new Action(() => { DataGridTasks.ItemsSource = tasks; }));
                RefreshLabel(LabelStatusInfo, string.Format(Properties.Resources.String_TasksEnabled, DataGridTasks.SelectedItems.Count),
                    Brushes.Green);

                Dispatcher.Invoke(new Action(() => ((Button)sender).IsEnabled = true));
            })).Start();


        }

        private void ButtonDisableTask_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridTasks.SelectedItems == null) return;

            ((Button)sender).IsEnabled = false;
            var taskList = new List<TaskInformation>();
            foreach (TaskInformation t in DataGridTasks.SelectedItems) taskList.Add(t);

            new Task(new Action(() =>
            {
                RefreshLabel(LabelStatusInfo, string.Format(Properties.Resources.String_DisablingTasks, DataGridTasks.SelectedItems.Count),
                    Brushes.Red);
                foreach (TaskInformation item in taskList)
                {
                    try
                    {
                        Helper.Instance.DisableTask(null, item.TaskName);
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show(
                            string.Format(Properties.Resources.String_DisableTasksError, item.TaskName, exc.Message),
                            Properties.Resources.Generic_Error);
                    }
                }

                var tasks = Helper.Instance.GetScheduledTasks(_selectedServer);
                Dispatcher.Invoke(new Action(() => { DataGridTasks.ItemsSource = tasks; }));
                RefreshLabel(LabelStatusInfo, string.Format(Properties.Resources.String_TasksDisabled, DataGridTasks.SelectedItems.Count),
                    Brushes.Green);

                Dispatcher.Invoke(new Action(() => ((Button)sender).IsEnabled = true));
            })).Start();
        }

        private void ListViewEventLogs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ListViewEventLogs.SelectedItem == null) return;

            string log = ListViewEventLogs.SelectedItem.ToString();
            EventEntries = new ObservableCollection<EventEntryInfo>();
            DataGridEventLogEntries.ItemsSource = EventEntries;

            new Task(new Action(() =>
            {
                RefreshLabel(LabelStatusInfo,
                    string.Format(Properties.Resources.String_RetrievingEvents, log), Brushes.Red);

                var eventList = Helper.Instance.GetEvents(log, _selectedServer);

                Dispatcher.Invoke(
                new Action(
                () =>
                {
                    eventList.ForEach(entry => EventEntries.Add(entry));
                }
                ));
            })).Start();
        }

        private void ButtonKillProcessMultipleMachines_Click(object sender, RoutedEventArgs e)
        {
            KillProcess kp = new KillProcess();
            _machineListViews.ForEach(x =>
            {
                foreach (string item in x.SelectedItems)
                {
                    kp.MachineStatus.Add(item, false);
                }
            });
            kp.DataGridKillResults.Items.Refresh();
            kp.Show();
        }

        private void ButtonChangeServiceMultipleMachines_Click(object sender, RoutedEventArgs e)
        {
            ChangeServices cs = new ChangeServices();
            _machineListViews.ForEach(x =>
            {
                foreach (string item in x.SelectedItems)
                {
                    cs.MachineStatus.Add(item, false);
                }
            });
            cs.DataGridServiceStatus.Items.Refresh();
            cs.Show();
        }

        private void TextBoxEventFilter_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxEventFilter.Text))
            {
                DataGridEventLogEntries.ItemsSource = EventEntries;
                return;
            }

            string text = TextBoxEventFilter.Text;

            DataGridEventLogEntries.ItemsSource = EventEntries.Where(entry =>
            Regex.IsMatch(entry.EventId.ToString(), text, RegexOptions.IgnoreCase) ||
            Regex.IsMatch(entry.EventMessage, text, RegexOptions.IgnoreCase) ||
            Regex.IsMatch(entry.EventSource, text, RegexOptions.IgnoreCase));
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            //TODO create wiki/doc and link to it
            Process.Start("http://about:blank");
        }
    }
}
