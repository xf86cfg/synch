using System.Net;
using System;


namespace synch
{
    public class DnsServiceConfig
    {
        public string ListenAddress { get; set; } //IP
        public string Domain { get; set; } //FQDN
        public string Record { get; set; } //FQDN
        public int RecordTtl { get; set; } //32 bit
        public string TargetNormal { get; set; } //IP
        public string TargetFailover { get; set; } //IP
        public string Forwarder { get; set; } //IP
        public int RefreshInterval { get; set; } //32 bit
        public int RetryInterval { get; set; } //32 bit 
        public int ExpireInterval { get; set; } //32 bit
        public int MinTTL { get; set; } //32 bit
        public int TTL { get; set; } //32 bit
        public ManagedServiceState DefaultState { get; set; }

        public static void Validate(DnsServiceConfig config)
        {
            if (!Utils.IsIPAddress(config.ListenAddress))
                throw new FormatException($"Failed to convert {config.ListenAddress} to IP address");
            if (!Utils.IsDomain(config.Domain))
                throw new FormatException($"Failed to convert {config.Domain} to domain name");
            if (!Utils.IsDomain(config.Record))
                throw new FormatException($"Failed to convert {config.Record} to record name");
            if (config.RecordTtl < 0)
                throw new FormatException($"Failed to convert {config.RecordTtl} to refresh interval");
            if (!Utils.IsIPAddress(config.TargetNormal))
                throw new FormatException($"Failed to convert {config.TargetNormal} to IP address");
            if (!Utils.IsIPAddress(config.TargetFailover))
                throw new FormatException($"Failed to convert {config.TargetFailover} to IP address");
            if (!Utils.IsIPAddress(config.Forwarder))
                throw new FormatException($"Failed to convert {config.Forwarder} to IP address");
            if (config.RefreshInterval < 0)
                throw new FormatException($"Failed to convert {config.RefreshInterval} to refresh interval");
            if (config.RetryInterval < 0)
                throw new FormatException($"Failed to convert {config.RetryInterval} to retry interval");
            if (config.ExpireInterval < 0)
                throw new FormatException($"Failed to convert {config.ExpireInterval} to expire interval");
            if (config.MinTTL < 0)
                throw new FormatException($"Failed to convert {config.MinTTL} to minimum TTL");
            if (config.TTL < 0)
                throw new FormatException($"Failed to convert {config.TTL} to TTL");
        }
    }


}
