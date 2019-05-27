using System;
using System.Collections.Generic;

namespace synch
{
    public enum ProbeMonitorState
    {
        Init,
        Unstable,
        Failed,
        Stable
    }

    public interface IProbeMonitor : IDisposable
    {
        ProbeMonitorState MonitorState { get; }
        event EventHandler<ProbeMonitortStateChangedEventArgs> MonitorStateChanged;
        void Start();
        void Stop();
    }

    public class ProbeMonitortStateChangedEventArgs : EventArgs
    {
        public ProbeMonitorState OldState { get; set; }
        public ProbeMonitorState NewState { get; set; }
        public Dictionary<string, string> Attributes { get; set; }
        public ProbeMonitortStateChangedEventArgs(ProbeMonitorState oldState, ProbeMonitorState newState, Dictionary<string, string> attributes = null)
        {
            OldState = oldState;
            NewState = newState;
            Attributes = attributes;
        }
    }
}
