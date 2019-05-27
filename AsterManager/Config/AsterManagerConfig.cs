using System;
using System.Collections.Generic;
using System.Text;

namespace synch
{
    public class AsterManagerConfig
    {
        virtual public string Hostname { get; set; }
        virtual public int Port { get; set; }
        virtual public string Username { get; set; }
        virtual public string Password { get; set; }

        public static void Validate (AsterManagerConfig config)
        {
            if (!Utils.IsDomain(config.Hostname) && !Utils.IsIPAddress(config.Hostname))
                throw new FormatException($"Failed to convert {config.Hostname} to hostname or IP address");
            if (!Utils.IsPort(config.Port))
                throw new FormatException($"Failed to convert {config.Port} to port");
            if (string.IsNullOrEmpty(config.Username))
                throw new FormatException($"Failed to convert null or empty value to username");
            if (string.IsNullOrEmpty(config.Password))
                throw new FormatException($"Failed to convert null or empty value to password");
        }
    }
}
