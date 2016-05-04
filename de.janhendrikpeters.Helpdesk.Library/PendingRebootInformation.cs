namespace de.janhendrikpeters.helpdesk.library
{
    public class PendingRebootInformation
    {
        private bool _rebootPending;
        private bool _isCcmReboot;
        private bool _isWindowsUpdateReboot;
        private string _pendingFileRenames;

        public string PendingFileRenames
        {
            get { return _pendingFileRenames; }
            set { _pendingFileRenames = value; }
        }


        public bool IsWindowsUpdateReboot
        {
            get { return _isWindowsUpdateReboot; }
            set { _isWindowsUpdateReboot = value; }
        }


        public bool IsCcmReboot
        {
            get { return _isCcmReboot; }
            set { _isCcmReboot = value; }
        }


        public bool RebootPending
        {
            get { return _rebootPending; }
            set { _rebootPending = value; }
        }

        public PendingRebootInformation()
        {
            RebootPending = false;
            IsCcmReboot = false;
            IsWindowsUpdateReboot = false;
            PendingFileRenames = string.Empty;
        }

        public PendingRebootInformation(bool rebootPending, bool isCcmReboot, bool isWindowsUpdateReboot, string pendingFileRenames)
        {
            RebootPending = rebootPending;
            IsCcmReboot = isCcmReboot;
            IsWindowsUpdateReboot = IsWindowsUpdateReboot;
            PendingFileRenames = pendingFileRenames;
        }
    }
}
