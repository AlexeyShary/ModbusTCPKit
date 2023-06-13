using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Threading;

namespace ModbusTCPClient
{
    public class Logger
    {
        SynchronizationContext uiContext;

        public Logger()
        {
            uiContext = SynchronizationContext.Current;
        }

        ObservableCollection<LogEntry> logEntries = new ObservableCollection<LogEntry>();
        public ObservableCollection<LogEntry> LogEntries
        {
            get { return logEntries; }
            private set { logEntries = value; }
        }

        public void AddMessage(string newMessage)
        {
            string timeStamp = DateTime.Now.ToLongTimeString() + "." + DateTime.Now.Millisecond.ToString();

            uiContext.Send(x => LogEntries.Add(new LogEntry(timeStamp, newMessage)), null);
        }

        public class LogEntry
        {
            public LogEntry(string timeStamp, string message)
            {
                TimeStamp = timeStamp;
                Message = message;
            }

            string timeStamp;
            public string TimeStamp
            {
                get { return timeStamp; }
                private set { timeStamp = value; }
            }

            string message;
            public string Message
            {
                get { return message; }
                private set { message = value; }
            }

            public string FormatedMessage
            {
                get { return String.Format("{0} \t {1}", TimeStamp, Message); }
            }
        }
    }
}
