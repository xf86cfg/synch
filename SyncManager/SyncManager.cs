using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Timers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace synch
{ 
    public class SyncManager : ISyncManager
    {
        public string JobId { get; private set; }
        public bool SyncInProgress { get; private set; }
        private volatile SyncState _previousState;
        private volatile SyncState _syncState;
        
        public SyncState SyncState
        {
            get
            {
                return _syncState;
            }
            private set
            {
                _previousState = _syncState;
                _syncState = value;
                if (_previousState != _syncState)
                {
                    _logger.LogInformation($"Sync state changed: {_previousState} => {_syncState}");
                    SyncStateChanged?.Invoke(this, new SyncManagerStateChangedEventArgs(_previousState, _syncState, null));
                }
                else
                {
                    _logger.LogTrace($"Sync state: {_syncState}");
                }
            }
        }

        private Timer _timer;
        private IList<string> _remoteConfig;
        private IList<string> _localConfig;
        private AsterManager _remoteManager;
        private AsterManager _localManager;
        private ILogger<SyncManager> _logger;
        private volatile bool _backupLocalConfig = true;
        public event EventHandler<SyncManagerStateChangedEventArgs> SyncStateChanged;
        private string _remoteFilename;
        private string _localFilename;
        private KeyValuePair<string, string> _filter;
        private string _localConfigsPath;
        private string _workDirectory;
        private bool _disposed = false;


        public SyncManager(ILogger<SyncManager> logger, IOptions<SyncManagerConfig> options ,IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            SyncManagerConfig.Validate(options.Value);
            _remoteFilename = options.Value.RemoteFilename;
            _localFilename = options.Value.LocalFilename;
            _filter = new KeyValuePair<string, string>(options.Value.FilterKey, options.Value.FilterValue);
            _localConfigsPath = options.Value.LocalConfigsPath;
            _workDirectory = options.Value.WorkDirectory;
            var scope = scopeFactory.CreateScope();
            _remoteManager = scope.ServiceProvider.GetRequiredService<AsterManagerRemote>();
            _localManager = scope.ServiceProvider.GetRequiredService<AsterManagerLocal>();
            _timer = new Timer(TimeSpan.FromSeconds(options.Value.Interval).TotalMilliseconds);
            _timer.Elapsed += TimerElapsed;
            SetupLocalDirectory(_workDirectory);
        }

        public void CheckSyncState()
        {
            _logger.LogTrace("Initiating checking sync");
            if (SyncInProgress)
            {
                _logger.LogWarning("SyncManager is busy. Skipping this iteration");
                return;
            }
            SyncInProgress = true;
            try
            {
                _logger.LogTrace("Getting configs");
                _localConfig = GetLocalConfig(_localFilename);
                _remoteConfig = GetRemoteConfig(_remoteFilename);

                if (_remoteConfig == null)
                {
                    SyncInProgress = false;
                    throw new InvalidOperationException("Remote config doesn't exist or empty");
                }
                    
                if (_localConfig == null)
                {
                    _logger.LogWarning("Local config doesn't exist but it will be created during next successfull sync");
                    _backupLocalConfig = false;
                    _logger.LogInformation("Diff found");
                    SyncInProgress = false;
                    SyncState = SyncState.Unsynced;
                }
                else if (!_remoteConfig.SequenceEqual(_localConfig))
                {
                    _logger.LogInformation("Diff found");
                    SyncInProgress = false;
                    SyncState = SyncState.Unsynced;
                }
                else
                {
                    _logger.LogTrace("No diff found");
                    SyncInProgress = false;
                    SyncState = SyncState.Synced;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"CheckSyncState has failed to complete: {e.Message}");
                SyncState = SyncState.Init;
            }
            finally
            {
                SyncInProgress = false;
            }
        }

        public void InitiateSync()
        {
            JobId = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            _logger.LogInformation($"Initiating sync. JobId: {JobId}");
            if (SyncInProgress)
            {
                _logger.LogWarning("SyncManager is busy. Skipping this iteration");
                return;
            }
            SyncInProgress = true;
            try
            {
                SetupLocalDirectory(_workDirectory);
                if (_remoteConfig == null)
                {
                    throw new InvalidOperationException("Remote config cannot be null. Run CheckSyncState to retrieve remote and local configs");
                }

                if (_backupLocalConfig)
                {
                    BackupLocalConfig(_localFilename, RelativeConfigPath($"{_localFilename}.backup_{JobId}"));
                    _backupLocalConfig = false;
                }
                CreateLocalConfigCandidate(_remoteConfig, AbsoluteConfigPath($"{_localFilename}.candidate_{JobId}"));
                UpdateLocalConfig(RelativeConfigPath($"{_localFilename}.candidate_{JobId}"), _localFilename);
                SyncState = SyncState.Synced;
            }
            catch (Exception e)
            {
                _logger.LogError($"Sync job {JobId} has failed to complete: {e.Message}");
                SyncState = SyncState.Init;
            }
            finally
            {
                SyncInProgress = false;
                JobId = null; //
            }
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            _logger.LogTrace("Timer elapsed");
            CheckSyncState();
        }

        public void Start()
        {
            _timer.Start();
            _logger.LogInformation("Service has been started");
        }

        public void Stop()
        {
            _timer.Stop();
            _logger.LogInformation("Service has been stopped");
        }

        private IList<string> GetRemoteConfig(string filename)
        {
            _logger.LogTrace($"Getting remote config {filename}");
            return _remoteManager.GetConfig(filename, _filter);
        }

        private IList<string> GetLocalConfig(string filename)
        {
            _logger.LogTrace($"Getting local config {filename}");
            return _localManager.GetConfig(filename);
        }

        private void UpdateLocalConfig(string sourceFilename, string destinationFilename)
        {
            _logger.LogInformation($"Updating local config {destinationFilename} from {sourceFilename}");
            _localManager.UpdateConfig(sourceFilename, destinationFilename);
        }

        private void BackupLocalConfig(string filename, string backupFilename)
        {
            _localManager.BackupConfig(filename, backupFilename);
        }

        private void CreateLocalConfigCandidate(IList<string> config, string candidateFilename)
        {
            _logger.LogInformation($"Creating local candidate config: {candidateFilename}");
            File.WriteAllLines(candidateFilename, config);
        }
        
        private void SetupLocalDirectory(string directory)
        {
            var fullPath = $"{_localConfigsPath}{directory}";
            _logger.LogTrace($"Checking if work directory {fullPath} exist");
            if (!Directory.Exists(fullPath))
            {
                _logger.LogInformation($"Work directory {fullPath} doesn't exist. Creating.");
                Directory.CreateDirectory(fullPath);
                _logger.LogInformation($"Work directory {fullPath} has been created.");
            }
        }

        private string AbsoluteConfigPath(string filename = null)
        {
            return $"{_localConfigsPath}{_workDirectory}/{filename}";
        }

        private string RelativeConfigPath(string filename)
        {
            return $"{_workDirectory}/{filename}";
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
                    _timer?.Dispose();
                    _localManager?.Dispose();
                    _remoteManager?.Dispose();
                    _disposed = true;
                }
            }
        }
    }
}
