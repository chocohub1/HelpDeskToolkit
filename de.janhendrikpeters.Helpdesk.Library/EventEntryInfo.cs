using System;

namespace de.janhendrikpeters.helpdesk.library
{
    public class EventEntryInfo
    {
        private long _id;

        public long EventId
        {
            get { return _id; }
            set { _id = value; }
        }

        private string _source;

        public string EventSource
        {
            get { return _source; }
            set { _source = value; }
        }

        private string _eventMessage;

        public string EventMessage
        {
            get { return _eventMessage; }
            set { _eventMessage = value; }
        }

        private DateTime _dateLoggeDateTime;

        public DateTime DateLogged
        {
            get { return _dateLoggeDateTime; }
            set { _dateLoggeDateTime = value; }
        }
    }
}
