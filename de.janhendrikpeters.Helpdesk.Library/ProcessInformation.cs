using System;

namespace de.janhendrikpeters.helpdesk.library
{
    public class ProcessInformation
    {
        private string _name;
        private string _userName;
        private int _pid;
        private string _executablePath;
        private string _commandLine;
        private double _privateWorkingSetMb;



        public int ProcessId
        {
            get { return _pid; }
            set { _pid = value; }
        }
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
        public double ProcessMemory
        {
            get { return _privateWorkingSetMb; }
            set { _privateWorkingSetMb = value; }
        }
        public string UserName
        {
            get { return _userName; }
            set { _userName = value; }
        }
        public string ExecutablePath
        {
            get { return _executablePath; }
            set { _executablePath = value; }
        }
        public string CommandLine
        {
            get { return _commandLine; }
            set { _commandLine = value; }
        }

        public ProcessInformation(int pid, string name, string username, long memory, string serverName, bool useExtendedProcess)
        {
            ProcessId = pid;
            Name = name;
            UserName = username;
            ProcessMemory = memory.ToMegabyte();

            if (!useExtendedProcess) return;
            if (!string.IsNullOrWhiteSpace(UserName)) return;

            try
            {
                System.Management.ManagementScope scope = new System.Management.ManagementScope($"\\\\{serverName}\\root\\cimv2");
                System.Management.ObjectQuery query = new System.Management.ObjectQuery($"SELECT * FROM Win32_Process WHERE ProcessId = '{pid}'");
                scope.Connect();
                System.Management.ManagementObjectSearcher Processes = new System.Management.ManagementObjectSearcher(scope, query);

                foreach (System.Management.ManagementObject Process in Processes.Get())
                {
                    if (Process["ExecutablePath"] != null)
                    {
                        ExecutablePath = Process["ExecutablePath"].ToString();

                        string[] OwnerInfo = new string[2];
                        Process.InvokeMethod("GetOwner", (object[])OwnerInfo);

                        UserName = OwnerInfo[0];
                    }

                    if (Process["CommandLine"] != null)
                    {
                        CommandLine = Process["CommandLine"].ToString();
                    }
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }
        }
    }
}
