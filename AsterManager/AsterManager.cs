using System;
using System.Collections.Generic;
using AsterNET.Manager;
using AsterNET.Manager.Action;
using AsterNET.Manager.Response;
using Microsoft.Extensions.Options;

namespace synch
{
    public class AsterManager : IProbe, IDisposable
    {
        public string ProbeId { get; private set; }
        public bool ProbingInProgress { get; private set; }
        public event EventHandler<ProbeInitiatedEventArgs> ProbeInitiated;
        public event EventHandler<ProbeFinishedEventArgs> ProbeFinished;
        public event EventHandler<ProbeErroredEventArgs> ProbeErrored;
        private ManagerConnection _managerConnection;
        private bool _disposed = false;

        public AsterManager(IOptions<AsterManagerConfig> options)
        {
            AsterManagerConfig.Validate(options.Value);
            _managerConnection = new ManagerConnection(
                options.Value.Hostname,
                options.Value.Port,
                options.Value.Username,
                options.Value.Password
                );
        }

        public IList<string> GetConfig(string filename, KeyValuePair<string, string> filter = new KeyValuePair<string, string>())
        {
            IList<string> config;
            try
            {
                var response = SendRequest(new GetConfigAction(filename));
                if (!response.IsSuccess()) return null; //File doesn't exist
                var r = response as GetConfigResponse;
                if (r.Attributes == null || r.Categories == null) return new List<string>(); //File empty
                var p = new AsterConfParser();
                var parsedConfig =
                    filter.Key == null || filter.Value == null ?
                    p.ParseConfigAttributes(r.Categories, r.Attributes) :
                    p.ParseConfigAttributes(r.Categories, r.Attributes, filter);
                var b = new AsterConfBuilder();
                config = b.BuildConfig(parsedConfig);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Failed to get config {filename}", e);
            }
            return config;
        }

        public void BackupConfig(string filename, string backupFilename)
        {
            try
            {
                UpdateConfig(filename, backupFilename, reload: false);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Failed to backup config {filename} => {backupFilename}", e);
            }
        }

        public void UpdateConfig(string sourceFilename, string destinationFilename, bool reload = true)
        {
            try
            {
                var r = SendRequest(new UpdateConfigAction(sourceFilename, destinationFilename, reload));
                if (!r.IsSuccess()) throw new InvalidOperationException("Target response wasn't successfull");
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Failed to update config {sourceFilename} => {destinationFilename}", e);
            }
        }

        public ManagerResponse SendRequest(ManagerAction request) // refactor
        {
            if (!_managerConnection.IsConnected()) _managerConnection.Login();
            return _managerConnection.SendAction(request);
        }

        public void InitiateProbing()
        {
            ProbeId = $"Probe{DateTime.Now.ToString("yyyyMMddHHmmssffff")}";
            ProbingInProgress = true;
            ProbeInitiated?.Invoke(this, new ProbeInitiatedEventArgs(ProbeId));
            try
            {
                var r = SendRequest(new PingAction());
                ProbeFinished?.Invoke(this, new ProbeFinishedEventArgs(ProbeId, r.IsSuccess()));
            }
            catch (Exception e)
            {
                ProbeErrored?.Invoke(this, new ProbeErroredEventArgs(ProbeId, new Exception("Probe has failed", e)));
            }
            finally
            {
                ProbingInProgress = false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_managerConnection.IsConnected()) _managerConnection.Logoff();
                    _disposed = true;
                }
            }
        }
    }
}
