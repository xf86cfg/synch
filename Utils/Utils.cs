using System;
using System.Net;

namespace synch
{
    public static class Utils
    {
        public static bool IsIPAddress(string ipString)
        {
            return IPAddress.TryParse(ipString, out _);
        }

        public static bool IsHostname(string hostString)
        {
            return Uri.CheckHostName(hostString) == UriHostNameType.Basic ? true : false;
        }

        public static bool IsDomain(string domainString)
        {
            return Uri.CheckHostName(domainString) == UriHostNameType.Dns ? true : false;
        }

        public static bool IsPort(int port)
        {
            return port > 0 && port <= UInt16.MaxValue;
        }
    }
}
