using System;
using System.Net;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using DNS.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace synch
{
    public class DnsService : IManagedService
    {
        private MasterFile _masterFile;
        private DnsServer _server;
        private ILogger<DnsService> _logger;
        private IOptions<DnsServiceConfig> _options;
        private volatile ManagedServiceState _previousState;
        private volatile ManagedServiceState _serviceState;
        public event EventHandler<ManagedServiceStateChangedEventArgs> ServiceStateChanged;
        private int _defaultDnsPort = 53;
        private bool _disposed = false;

        public ManagedServiceState ServiceState
        {
            get
            {
                return _serviceState;
            }
            private set
            {
                _previousState = _serviceState;
                _serviceState = value;
                if (_previousState != _serviceState)
                {
                    _logger.LogInformation($"Service state changed: {_previousState} => {_serviceState}");
                    ServiceStateChanged?.Invoke(this, new ManagedServiceStateChangedEventArgs(_previousState, _serviceState, null));
                }
                else
                {
                    _logger.LogTrace($"Service state: {_serviceState}");
                }
            }
        }

        public DnsService(IOptions<DnsServiceConfig> options, ILogger<DnsService> logger)
        {
            _logger = logger;
            DnsServiceConfig.Validate(options.Value);
            _options = options;
            ConfigureService();
            InitState(_options.Value.DefaultState);
        }

        public void Start()
        {
            _server.Listen(_defaultDnsPort, IPAddress.Parse(_options.Value.ListenAddress));
            _logger.LogInformation($"Service has been started {_options.Value.ListenAddress}:{_defaultDnsPort}");
        }

        public void Stop()
        {
            _server.Dispose();
            ConfigureService();
            InitState(_options.Value.DefaultState);
            _logger.LogInformation("Service has been stopped");
        }

        private void ConfigureService()
        {
            _masterFile = new MasterFile();
            var soaoptions = new StartOfAuthorityResourceRecord.Options()
            {
                SerialNumber = GenerateSerialNumber(),
                RefreshInterval = new TimeSpan(0, 0, _options.Value.RefreshInterval),
                RetryInterval = new TimeSpan(0, 0, _options.Value.RetryInterval),
                ExpireInterval = new TimeSpan(0, 0, _options.Value.ExpireInterval),
                MinimumTimeToLive = new TimeSpan(0, 0, _options.Value.MinTTL)
            };
            var domain = new Domain(_options.Value.Domain);
            var soa = new StartOfAuthorityResourceRecord(
                domain, domain, domain, soaoptions, new TimeSpan(0, 0, _options.Value.TTL));
            _masterFile.Add(soa);
            var record = new IPAddressResourceRecord(
                new Domain(GetRecordFqdn()), IPAddress.Parse(_options.Value.TargetNormal), new TimeSpan(0, 0, _options.Value.RecordTtl));
            _masterFile.Add(record);
            _server = new DnsServer(_masterFile, _options.Value.Forwarder);
            _server.Listening += ServerListening;
            _server.Responded += ServerResponded;
            _server.Errored += ServerErrored;
        }

        public void InitNormalState()
        {
            InitState(ManagedServiceState.Normal);
        }

        public void InitFailoverState()
        {
            InitState(ManagedServiceState.Failover);
        }

        private void InitState(ManagedServiceState state)
        {
            _logger.LogTrace($"Initiating {state} state");
            if (state != ManagedServiceState.Normal && state != ManagedServiceState.Failover)
            {
                _logger.LogError("Wrong state was requested to initiate");
                return;
            }
            if (state == ManagedServiceState.Normal)
            {
                _masterFile.UpdateIPAddressResourceRecord(GetRecordFqdn(), _options.Value.TargetFailover, _options.Value.TargetNormal);
            }
            if (state == ManagedServiceState.Failover)
            {
                _masterFile.UpdateIPAddressResourceRecord(GetRecordFqdn(), _options.Value.TargetNormal, _options.Value.TargetFailover);
            }
            _masterFile.IncreaseSoaSerialNumber(_options.Value.Domain);
            ServiceState = state;
            _logger.LogInformation($"Switched to {state} state");
        }

        private void ServerListening(object sender, EventArgs args)
        {
            _logger.LogInformation("Service is listening");
        }

        private void ServerResponded(object sender, DnsServer.RespondedEventArgs args)
        {
            _logger.LogTrace($"Service has responded:\n {args.Request}, {args.Response}");
        }

        private void ServerErrored(object sender, DnsServer.ErroredEventArgs args)
        {
            _logger.LogError($"Service has errored: {args.Exception.Message}");
        }

        private static long GenerateSerialNumber()
        {
            return long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
        }

        private string GetRecordFqdn()
        {
            return $"{_options.Value.Record}.{_options.Value.Domain}";
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
                    _server?.Dispose();
                    _disposed = true;
                }
            }
        }
    }
}
