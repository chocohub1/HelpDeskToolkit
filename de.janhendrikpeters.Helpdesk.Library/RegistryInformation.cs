using System;

namespace de.janhendrikpeters.helpdesk.library
{
    public class RegistryInformation
    {
        private string _displayName;
        private Version _displayVersion;

        public string DisplayName
        {
            get { return _displayName; }
            set { _displayName = value; }
        }
        public Version DisplayVersion
        {
            get { return _displayVersion; }
            set { _displayVersion = value; }
        }

        public RegistryInformation()
        {

        }

        public RegistryInformation(string displayName, Version displayVersion)
        {
            DisplayName = displayName;
            DisplayVersion = displayVersion;
        }
    }
}
