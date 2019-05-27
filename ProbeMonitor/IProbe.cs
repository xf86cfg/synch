using System;
using System.Collections.Generic;

namespace synch
{
    public interface IProbe : IDisposable
    {
        string ProbeId { get; }
        bool ProbingInProgress { get; }
        event EventHandler<ProbeInitiatedEventArgs> ProbeInitiated;
        event EventHandler<ProbeFinishedEventArgs> ProbeFinished;
        event EventHandler<ProbeErroredEventArgs> ProbeErrored;
        void InitiateProbing();
    }

    public class ProbeInitiatedEventArgs : EventArgs
    {
        public string ProbeId { get; private set; }
        public Dictionary<string, string> Attributes { get; set; }
        public ProbeInitiatedEventArgs(string probeId, Dictionary<string, string> attributes = null)
        {
            ProbeId = probeId;
            Attributes = attributes;
        }
    }

    public class ProbeFinishedEventArgs : EventArgs
    {
        public string ProbeId { get; private set; }
        public bool IsSuccess { get; private set; }
        public Dictionary<string, string> Attributes { get; set; }
        public ProbeFinishedEventArgs(string probeId, bool isSuccess, Dictionary<string, string> attributes = null)
        {
            ProbeId = probeId;
            IsSuccess = isSuccess;
            Attributes = attributes;
        }
    }

    public class ProbeErroredEventArgs : EventArgs
    {
        public Exception Exception { get; private set; }
        public string ProbeId { get; private set; }
        public Dictionary<string, string> Attributes { get; set; }
        public ProbeErroredEventArgs(string probeId, Exception e, Dictionary<string, string> attributes = null)
        {
            ProbeId = probeId;
            Exception = e;
            Attributes = attributes;
        }
    }
}
