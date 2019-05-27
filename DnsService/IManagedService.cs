using System;
using System.Collections.Generic;
using System.Text;

namespace synch
{
    public enum ManagedServiceState
    {
        Init,
        Normal,
        Failover
    }

    public interface IManagedService : IDisposable
    {
        ManagedServiceState ServiceState { get; }
        event EventHandler<ManagedServiceStateChangedEventArgs> ServiceStateChanged;
        void Start();
        void Stop();
        void InitNormalState();
        void InitFailoverState();
    }

    public class ManagedServiceStateChangedEventArgs : EventArgs
    {
        public ManagedServiceState OldState { get; set; }
        public ManagedServiceState NewState { get; set; }
        public Dictionary<string, string> Attributes { get; set; }
        public ManagedServiceStateChangedEventArgs(ManagedServiceState oldState, ManagedServiceState newState, Dictionary<string, string> attributes = null)
        {
            OldState = oldState;
            NewState = newState;
            Attributes = attributes;
        }
    }
}
