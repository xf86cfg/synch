using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace synch
{
    public class SynchService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private IProbeMonitor _probeMonitor;
        private ISyncManager _syncManager;
        private IManagedService _managedService;
        private IServiceControl _serviceControl;


        public SynchService(ILogger<SynchService> logger, IServiceScopeFactory scopeFactory)
        {
            try
            {
                _logger = logger;
                SetupEssentials(scopeFactory);
            }
            catch (Exception e)
            {
                _logger.LogCritical($"Failed to initialize service: {e.Message}");
                StopAsync(new CancellationToken(true));
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Starting service");
            _probeMonitor.Start();
            _syncManager.Start();
            _managedService.Start();
            _serviceControl.Start();            
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping service");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposing service");
            _probeMonitor?.Dispose();
            _syncManager?.Dispose();
            _managedService?.Dispose();
        }

        private void SetupEssentials(IServiceScopeFactory scopeFactory)
        {
            _logger.LogInformation("Setting up essentials");
            var scope = scopeFactory.CreateScope();

            _probeMonitor = scope.ServiceProvider.GetRequiredService<ProbeMonitor>();
            _probeMonitor.MonitorStateChanged += MonitorStateChanged;

            _syncManager = scope.ServiceProvider.GetRequiredService<SyncManager>();
            _syncManager.SyncStateChanged += SyncStateChanged;

            _managedService = scope.ServiceProvider.GetRequiredService<DnsService>();
            _managedService.ServiceStateChanged += ServiceStateChanged;

            _serviceControl = scope.ServiceProvider.GetRequiredService<ServiceControl>();
            _serviceControl.ServiceControlRequest += ServiceControlActionRequest;

            _logger.LogInformation("Essentials have been set up");
        }

        public void MonitorStateChanged(object sender, ProbeMonitortStateChangedEventArgs args)
        {
            var message = $"ProbeMonitor state changed: {args.OldState} => {args.NewState}";
            switch (args.NewState)
            {
                case ProbeMonitorState.Unstable:
                    _logger.LogWarning(message);
                    break;
                case ProbeMonitorState.Failed:
                    _logger.LogError(message);
                    _logger.LogInformation("Initiating failover mode");
                    _managedService.InitFailoverState();
                    break;
                case ProbeMonitorState.Stable:
                    _logger.LogInformation(message);
                    _logger.LogInformation("Initiating normal mode");
                    _managedService.InitNormalState();
                    break;
                default:
                    _logger.LogInformation(message);
                    break;
            }
        }

        public void SyncStateChanged(object sender, SyncManagerStateChangedEventArgs args)
        {
            _logger.LogInformation($"Sync state has changed: {args.OldState} => {args.NewState}");
            if (args.NewState == SyncState.Unsynced)
            {
                _logger.LogInformation("Initiating sync");
                _syncManager.InitiateSync();
            }
        }

        public void ServiceStateChanged(object sender, ManagedServiceStateChangedEventArgs args)
        {
            _logger.LogInformation($"Service state has changed: {args.OldState} => {args.NewState}");
        }

        public void ServiceControlActionRequest(object sender, ServiceControlRequestEventArgs args)
        {
            _logger.LogTrace($"Service control request received, action: {args.Action}");
            if (args.Action == "GetStatus")
            {
                args.IsSuccess = true;
                var response = new Dictionary<string, string>()
                {
                    { "Timestamp", DateTime.Now.ToString("yyyyMMddHHmmss")},
                    { "Monitor", _probeMonitor.MonitorState.ToString() },
                    { "Sync", _syncManager.SyncState.ToString() },
                    { "Service", _managedService.ServiceState.ToString() }
                };
                args.Responses.Add(response);
                _logger.LogTrace($"Service control response sent");
            }
        }
    }
}