using System;

namespace synch
{
    public class SyncManagerConfig
    {
        public string RemoteFilename { get; set; } //Not empty or null
        public string LocalFilename { get; set; } = "sip_synch.conf";
        public double Interval { get; set; } //>= 0
        public string FilterKey { get; set; } //Not null
        public string FilterValue { get; set; } //Not null
        public string LocalConfigsPath { get; set; } //Not empty or null
        public string WorkDirectory { get; set; } = "synch";

        public static void Validate(SyncManagerConfig config)
        {
            if (string.IsNullOrEmpty(config.RemoteFilename))
                throw new FormatException($"Failed to convert null or empty value to remote config filename");
            if (string.IsNullOrEmpty(config.LocalFilename))
                throw new FormatException($"Failed to convert null or empty value to local config filename");
            if (config.Interval < 0)
                throw new FormatException($"Failed to convert {config.Interval} to sync interval");
            if (config.FilterKey == null)
                throw new FormatException($"Failed to convert null to filter key");
            if (config.FilterValue == null)
                throw new FormatException($"Failed to convert null to filter value");
            if (string.IsNullOrEmpty(config.LocalConfigsPath))
                throw new FormatException($"Failed to convert null or empty value to local configs path");
            if (string.IsNullOrEmpty(config.WorkDirectory))
                throw new FormatException($"Failed to convert null or empty value to work directory");
        }
    }
}
