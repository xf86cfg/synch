using System;

namespace synch
{
    public class ProbeMonitorConfig
    {
        public int FailureTolerance { get; set; }
        public int AliveThreshold { get; set; }
        public int ErrorPenaltyPoints { get; set; }
        public double Interval { get; set; }

        public static void Validate(ProbeMonitorConfig config)
        {
            if (config.FailureTolerance < 0)
                throw new FormatException($"Cannot convert {config.FailureTolerance} to failure tolerance");
            if (config.AliveThreshold < 0)
                throw new FormatException($"Cannot convert {config.AliveThreshold} to alive threshold");
            if (config.ErrorPenaltyPoints < 0)
                throw new FormatException($"Cannot convert {config.ErrorPenaltyPoints} to error penalty points");
            if (config.Interval < 0)
                throw new FormatException($"Cannot convert {config.Interval} to probe interval");
        }
    }
}
