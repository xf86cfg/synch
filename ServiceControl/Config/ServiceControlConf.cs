using System;
using System.Collections.Generic;
using System.Text;

namespace synch
{
    public class ServiceControlConf
    {
        public string Token { get; set; }
        public string ListenAddress { get; set; }
        public int Port { get; set; }

        public static void Validate(ServiceControlConf config)
        {
            if (string.IsNullOrEmpty(config.Token))
                throw new FormatException($"Failed to convert null or empty value to token");
            if (!Utils.IsIPAddress(config.ListenAddress))
                throw new FormatException($"Failed to convert {config.ListenAddress} to IP address");
            if (!Utils.IsPort(config.Port))
                throw new FormatException($"Failed to convert {config.Port} to port");
        }
    }
}
