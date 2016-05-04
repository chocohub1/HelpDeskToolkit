using System;
using Microsoft.Win32.TaskScheduler;

namespace de.janhendrikpeters.helpdesk.library
{
    public class TaskInformation
    {
        private string _taskName;
        private TaskState _taskStatus;
        private bool _isEnabled;
        private DateTime _lastRunTime;
        private DateTime _nextRunTime;
        private int _lastRunResult;
        private int _numberOfMissedRuns;

        public string TaskName
        {
            get { return _taskName; }
            set { _taskName = value; }
        }
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { _isEnabled = value; }
        }
        public TaskState TaskStatus
        {
            get { return _taskStatus; }
            set { _taskStatus = value; }
        }

        public DateTime LastRunTime
        {
            get { return _lastRunTime; }
            set { _lastRunTime = value; }
        }

        public int LastRunResult
        {
            get { return _lastRunResult; }
            set { _lastRunResult = value; }
        }
        public int NumberOfMissedRuns
        {
            get { return _numberOfMissedRuns; }
            set { _numberOfMissedRuns = value; }
        }

        public DateTime NextRunTime
        {
            get { return _nextRunTime; }
            set { _nextRunTime = value; }
        }

        public TaskInformation()
        {

        }
    }
}
