using System.ServiceProcess;

namespace de.janhendrikpeters.helpdesk.library
{
    public class ServiceInformation
    {
        private string _serviceName;
        private string _serviceDisplayName;
        private ServiceControllerStatus _status;
        private string _startupMethod;
        public string ServiceName
        {
            get { return _serviceName; }
            set { _serviceName = value; }
        }
        public string DisplayName
        {
            get { return _serviceDisplayName; }
            set { _serviceDisplayName = value; }
        }
        public ServiceControllerStatus ServiceStatus
        {
            get { return _status; }
            set { _status = value; }
        }
        public string StartupMethod
        {
            get { return _startupMethod; }
            set { _startupMethod = value; }
        }

        public ServiceInformation(string serviceName, string serviceDisplayName, ServiceControllerStatus status)
        {
            ServiceName = serviceName;
            DisplayName = serviceDisplayName;
            ServiceStatus = status;
        }
    }
}
