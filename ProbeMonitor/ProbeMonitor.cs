using System;
using System.Timers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;


namespace synch
{
    public class ProbeMonitor : IProbeMonitor
    {
        private ILogger<ProbeMonitor> _logger;
        private IProbe _probe;
        private volatile ProbeMonitorState _previousState;
        private volatile ProbeMonitorState _monitorState = ProbeMonitorState.Init;
        public ProbeMonitorState MonitorState
        {
            get
            {
                return _monitorState;
            }
            private set
            {
                _previousState = _monitorState;
                _monitorState = value;
                if (_monitorState == ProbeMonitorState.Stable) _failed = 0;
                if (_previousState != _monitorState)
                {
                    _logger.LogTrace($"Monitor state changed: {_previousState} => {_monitorState}");
                    MonitorStateChanged?.Invoke(this, new ProbeMonitortStateChangedEventArgs(_previousState, _monitorState));
                }
            }
        }
        private volatile int _probeFailureTolerance;
        private volatile int _aliveThreshold;
        private volatile int _errorPenaltyPoints;
        private Timer _timer;
        private volatile int _failed = 0;
        private volatile int _succeed = 0;
        public event EventHandler<ProbeMonitortStateChangedEventArgs> MonitorStateChanged;
        private bool _disposed = false;

        public ProbeMonitor(ILogger<ProbeMonitor> logger, IOptions<ProbeMonitorConfig> options, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            ProbeMonitorConfig.Validate(options.Value);
            _probeFailureTolerance = options.Value.FailureTolerance;
            _aliveThreshold = options.Value.AliveThreshold;
            _errorPenaltyPoints = options.Value.ErrorPenaltyPoints;
            _probe = scopeFactory.CreateScope().ServiceProvider.GetRequiredService<AsterManagerRemote>();
            _probe.ProbeInitiated += ProbeInitiated;
            _probe.ProbeFinished += ProbeFinished;
            _probe.ProbeErrored += ProbeErrored;
            _timer = new Timer(TimeSpan.FromSeconds(options.Value.Interval).TotalMilliseconds);
            _timer.Elapsed += TimerElapsed;
        }

        public void Start()
        {
            _logger.LogTrace("Starting");
            _timer.Start();
        }

        public void Stop()
        {
            _logger.LogTrace("Stopping");
            _timer.Stop();
            Reset();
        }

        private void Reset()
        {
            _failed = 0;
            _succeed = 0;
            EvaluateState();
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            _logger.LogTrace("Timer elapsed. Trying to initiate probing");
            if (_probe.ProbingInProgress)
            {
                _logger.LogWarning("Last probe didn't finish yet, skipping");
            }
            else
            {
                _logger.LogTrace("Initiating probing");
                _probe.InitiateProbing();
            }
        }

        private void ProbeInitiated(object sender, ProbeInitiatedEventArgs args)
        {
            _logger.LogTrace($"Probe initiated, ID: {args.ProbeId}");
        }

        private void ProbeFinished(object sender, ProbeFinishedEventArgs args)
        {
            _logger.LogTrace($"Probe finished, ID: {args.ProbeId}");
            if (args.IsSuccess)
            {
                _logger.LogTrace($"Probe succeed, ID: {args.ProbeId}");
                ProbeSucceed();
            }
            else
            {
                _logger.LogWarning($"Probe failed, ID: {args.ProbeId}");
                ProbeFailed();
            }
        }

        private void ProbeErrored(object sender, ProbeErroredEventArgs args)
        {
            _logger.LogError($"Probe errored, ID: {args.ProbeId}");
            ProbeErrored();
        }

        private void ProbeSucceed()
        {
            _succeed++;
            EvaluateState();
        }

        private void ProbeFailed()
        {
            _succeed = 0;
            _failed++;
            EvaluateState();
        }

        private void ProbeErrored()
        {
            _succeed = 0;
            _failed++;
            _failed += _errorPenaltyPoints;
            EvaluateState();
        }

        private void EvaluateState()
        {
            if (_failed == 0 && _succeed == 0)
            {
                MonitorState = ProbeMonitorState.Init;
            }
            else if (_succeed >= _aliveThreshold)
            {
                MonitorState = ProbeMonitorState.Stable;
            }
            else if (_failed >= _probeFailureTolerance && _succeed == 0)
            {
                MonitorState = ProbeMonitorState.Failed;
            }
            else
            {
                MonitorState = ProbeMonitorState.Unstable;
            }
            _logger.LogTrace($"ProbeMonitorState: {MonitorState}");
            _logger.LogTrace($"Internal failed counter: {_failed}, succeed counter: {_succeed}");
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
                    _probe?.Dispose();
                    _disposed = true;
                }
            }
        }
    }
}
