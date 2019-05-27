using System;
using System.Collections.Generic;

namespace synch
{
    public enum SyncState
    {
        Init,
        Synced,
        Unsynced
    }

    public interface ISyncManager : IDisposable
    {
        string JobId { get; }
        bool SyncInProgress { get; }
        SyncState SyncState { get; }
        event EventHandler<SyncManagerStateChangedEventArgs> SyncStateChanged;
        void CheckSyncState();
        void InitiateSync();
        void Start();
        void Stop();
    }

    public class SyncManagerStateChangedEventArgs : EventArgs
    {
        public SyncState OldState { get; set; }
        public SyncState NewState { get; set; }
        public Dictionary<string, string> Attributes { get; set; }
        public SyncManagerStateChangedEventArgs(SyncState oldState, SyncState newState, Dictionary<string, string> attributes = null)
        {
            OldState = oldState;
            NewState = newState;
            Attributes = attributes;
        }
    }
}
