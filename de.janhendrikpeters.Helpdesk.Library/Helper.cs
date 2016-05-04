using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;

namespace de.janhendrikpeters.helpdesk.library
{
    public class Helper
    {
        /// <summary>
        /// ProcessHandler instance
        /// </summary>
        private static volatile Helper _instance;

        /// <summary>
        /// Lock object for thread-safety
        /// </summary>
        private static readonly object SyncRoot = new object();

        /// <summary>
        /// Prevents a default instance of the <see cref="Helper" /> class from being created.
        /// </summary>
        private Helper()
        {
            ServiceDictionary = new Dictionary<string, ServiceController>();
            _taskDictionary = new Dictionary<string, Microsoft.Win32.TaskScheduler.Task>();
            EventLogDictionary = new Dictionary<string, EventLog>();
            UseExtendedProcessInfo = Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["UseExtendedProcessInfo"]);
        }

        /// <summary>
        /// Gets the Singleton instance
        /// </summary>
        public static Helper Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        if (_instance == null)
                        {
                            _instance = new Helper();
                        }
                    }
                }

                return _instance;
            }
        }
        private Dictionary<string, ServiceController> _serviceDictionary;
        private Dictionary<string, Microsoft.Win32.TaskScheduler.Task> _taskDictionary;
        private PendingRebootInformation _currentPendingReboot;
        private Dictionary<string, EventLog> _eventlogDict;

        public bool UseExtendedProcessInfo { get; set; }

        public Dictionary<string, EventLog> EventLogDictionary
        {
            get { return _eventlogDict; }
            set { _eventlogDict = value; }
        }

        public PendingRebootInformation CurrentPendingReboot
        {
            get { return _currentPendingReboot; }
            set { _currentPendingReboot = value; }
        }

        public Dictionary<string, ServiceController> ServiceDictionary
        {
            get { return _serviceDictionary; }
            set { _serviceDictionary = value; }
        }


        public List<ProcessInformation> GetProcesses(string serverName)
        {
            return Process.GetProcesses(serverName).Select(x => new ProcessInformation(x.Id, x.ProcessName, x.StartInfo.UserName, x.PrivateMemorySize64, serverName, UseExtendedProcessInfo)).ToList();
        }

        public void KillProcesses(List<int> ids, string serverName)
        {
            string queryString = $"SELECT * FROM Win32_Process Where ProcessId = '{ids[0]}'";
            if (ids.Count > 1)
            {
                for (int i = 1; i < ids.Count; i++)
                {
                    queryString += $" or ProcessId = '{ids[i]}'";
                }
            }

            ManagementScope managementScope = new ManagementScope($"\\\\{serverName}\\root\\cimv2");
            managementScope.Connect();
            ObjectQuery objectQuery = new ObjectQuery(queryString);
            ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(managementScope, objectQuery);
            ManagementObjectCollection managementObjectCollection = managementObjectSearcher.Get();
            foreach (ManagementObject managementObject in managementObjectCollection)
            {
                managementObject.InvokeMethod("Terminate", null);
            }
        }

        public List<ServiceInformation> GetServices(string serverName)
        {
            ServiceDictionary = ServiceController.GetServices(serverName).ToDictionary(key => key.ServiceName, value => value);
            return ServiceDictionary.Select(x => new ServiceInformation(x.Value.ServiceName, x.Value.DisplayName, x.Value.Status)).ToList<ServiceInformation>();
        }

        public bool StopService(string serverName, string serviceName, bool useCache)
        {
            ServiceController svc;
            if (useCache)
            {
                svc = ServiceDictionary[serviceName];
            }
            else
            {
                svc = ServiceController.GetServices(serverName).First(x => x.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase) || x.DisplayName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
            }

            svc.Stop();
            svc.WaitForStatus(ServiceControllerStatus.Stopped);
            return svc.Status == ServiceControllerStatus.Stopped;
        }
        public bool StartService(string serverName, string serviceName, bool useCache)
        {
            ServiceController svc;
            if (useCache)
            {
                svc = ServiceDictionary[serviceName];
            }
            else
            {
                svc = ServiceController.GetServices(serverName).First(x => x.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase) || x.DisplayName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
            }

            svc.Start();
            svc.WaitForStatus(ServiceControllerStatus.Running);
            return svc.Status == ServiceControllerStatus.Running;
        }
        public bool RestartService(string serverName, string serviceName, bool useCache)
        {
            ServiceController svc;
            if (useCache)
            {
                svc = ServiceDictionary[serviceName];
            }
            else
            {
                var services = ServiceController.GetServices(serverName);
                svc = ServiceController.GetServices(serverName).First(x => x.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase) || x.DisplayName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
            }

            svc.Stop();
            svc.WaitForStatus(ServiceControllerStatus.Stopped);
            svc.Start();
            svc.WaitForStatus(ServiceControllerStatus.Running);
            return svc.Status == ServiceControllerStatus.Running;
        }

        public List<RegistryInformation> GetInstalledSoftware(string serverName)
        {
            string uninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            List<RegistryInformation> installedSoftware = new List<RegistryInformation>();

            using (RegistryKey rk = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, serverName).OpenSubKey(uninstallKey))
            {
                foreach (string skName in rk.GetSubKeyNames())
                {
                    using (RegistryKey sk = rk.OpenSubKey(skName))
                    {
                        RegistryInformation rg = new RegistryInformation();
                        try
                        {
                            rg.DisplayName = sk.GetValue("DisplayName").ToString();
                            if (string.IsNullOrWhiteSpace(rg.DisplayName)) continue;
                        }
                        catch (Exception ex)
                        {
                            continue;
                        }

                        try
                        {
                            Version tmp = new Version();
                            Version.TryParse(sk.GetValue("DisplayVersion").ToString(), out tmp);
                            rg.DisplayVersion = tmp;
                        }
                        catch (Exception ex)
                        { }
                        installedSoftware.Add(rg);
                    }
                }
            }

            return installedSoftware;
        }

        public List<TaskInformation> GetScheduledTasks(string serverName)
        {
            List<TaskInformation> tasks = new List<TaskInformation>();
            _taskDictionary = new Dictionary<string, Task>();
            using (TaskService ts = new TaskService(serverName))
            {
                foreach (var item in ts.AllTasks)
                {
                    if (!_taskDictionary.ContainsKey(item.Name))
                    {
                        _taskDictionary.Add(item.Name, item);
                    }

                    tasks.Add(new TaskInformation
                    {
                        TaskName = item.Name,
                        IsEnabled = item.Enabled,
                        LastRunResult = item.LastTaskResult,
                        LastRunTime = item.LastRunTime,
                        NextRunTime = item.NextRunTime,
                        NumberOfMissedRuns = item.NumberOfMissedRuns,
                        TaskStatus = item.State
                    });
                }
            }

            return tasks;
        }

        public List<string> GetEventLogs(string serverName)
        {
            List<string> outList = new List<string>();
            EventLogDictionary = new Dictionary<string, EventLog>();
            foreach (var log in EventLog.GetEventLogs(serverName))
            {
                if (EventLogDictionary.ContainsKey(log.Log)) continue;
                EventLogDictionary.Add(log.Log, log);
                outList.Add(log.Log);
            }
            return outList;
        }

        public List<EventEntryInfo> GetEvents(string logName, string serverName)
        {
            List<EventEntryInfo> infos = new List<EventEntryInfo>();

            using (EventLogSession session = new EventLogSession(serverName))
            {
                string queryString = $"<QueryList><Query Id=\"0\" Path=\"{logName}\"><Select Path=\"{logName}\">*[System[(Level = 1  or Level = 2 or Level = 3) and TimeCreated[timediff(@SystemTime) &lt;= 86400000]]]</Select></Query></QueryList>";

                EventLogQuery query = new EventLogQuery(logName, PathType.LogName, queryString);
                query.Session = session;
                EventLogReader reader = new EventLogReader(query);

                EventRecord eventRecord = null;
                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.TimeCreated != null)
                        infos.Add(new EventEntryInfo
                        {
                            EventId = eventRecord.Id,
                            EventMessage = eventRecord.FormatDescription(),
                            EventSource = eventRecord.ProviderName,
                            DateLogged = eventRecord.TimeCreated.Value
                        });
                }
            }

            return infos;
        }
        public void StartTask(string serverName, string taskName)
        {
            _taskDictionary[taskName].Run();
        }

        public void StopTask(string serverName, string taskName)
        {
            _taskDictionary[taskName].Stop();
        }

        public void EnableTask(string serverName, string taskName)
        {
            _taskDictionary[taskName].Enabled = true;
        }

        public void DisableTask(string serverName, string taskName)
        {
            _taskDictionary[taskName].Enabled = false;
        }

        public bool IsRebootPending(string serverName)
        {
            string componentBasedServicingKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\";
            string windowsUpdateKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\";
            string sessionManagerKey = @"SYSTEM\CurrentControlSet\Control\Session Manager\";

            CurrentPendingReboot = new PendingRebootInformation();

            using (RegistryKey rk = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, serverName).OpenSubKey(componentBasedServicingKey))
            {
                if (rk.GetSubKeyNames().Contains("RebootPending"))
                {
                    CurrentPendingReboot.RebootPending = true;
                    CurrentPendingReboot.IsCcmReboot = true;
                }
            }
            using (RegistryKey rk = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, serverName).OpenSubKey(windowsUpdateKey))
            {
                if (rk.GetSubKeyNames().Contains("RebootRequired"))
                {
                    CurrentPendingReboot.RebootPending = true;
                    CurrentPendingReboot.IsWindowsUpdateReboot = true;
                }
            }
            using (RegistryKey rk = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, serverName).OpenSubKey(sessionManagerKey))
            {
                if (rk.GetValue("PendingFileRenameOperations") == null) return CurrentPendingReboot.RebootPending;
                CurrentPendingReboot.RebootPending = true;
                CurrentPendingReboot.PendingFileRenames = rk.GetValue("PendingFileRenameOperations").ToString();
            }

            return CurrentPendingReboot.RebootPending;
        }

        public bool KillProcessByName(string processName, string machine)
        {
            string queryString = $"SELECT * FROM Win32_Process Where Name = '{processName}'";
            ManagementScope managementScope = new ManagementScope($"\\\\{machine}\\root\\cimv2");
            try
            {
                managementScope.Connect();
            }
            catch (System.Runtime.InteropServices.COMException exc)
            {
                // Connection to machine not possible
                Console.WriteLine(exc.Message);
                return false;
            }

            ObjectQuery objectQuery = new ObjectQuery(queryString);

            ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(managementScope, objectQuery);
            ManagementObjectCollection managementObjectCollection = managementObjectSearcher.Get();
            foreach (ManagementObject managementObject in managementObjectCollection)
            {
                try
                {
                    var result = managementObject.InvokeMethod("Terminate", null);
                    return true;
                }
                catch (Exception exc)
                {
                    Console.WriteLine(exc.Message);
                    return false;
                }
            }

            return true;
        }
    }
}