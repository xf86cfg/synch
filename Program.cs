using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace synch
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = new HostBuilder();
            builder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddEnvironmentVariables();
                config.AddJsonFile("appsettings.json", false, true);
                config.Build();
            });


            builder.ConfigureServices((hostContext, services) =>
            {
                services.AddOptions();
                services.Configure<ProbeMonitorConfig>(hostContext.Configuration.GetSection("probemonitor"));
                services.Configure<AsterManagerLocalConfig>(hostContext.Configuration.GetSection("amilocal"));
                services.Configure<AsterManagerRemoteConfig>(hostContext.Configuration.GetSection("amiremote"));
                services.Configure<SyncManagerConfig>(hostContext.Configuration.GetSection("syncmanager"));
                services.Configure<DnsServiceConfig>(hostContext.Configuration.GetSection("dnsservice"));
                services.Configure<ServiceControlConf>(hostContext.Configuration.GetSection("servicecontrol"));
                services.AddHostedService<SynchService>();
                services.AddScoped<ProbeMonitor>();
                services.AddScoped<AsterManagerLocal>();
                services.AddScoped<AsterManagerRemote>();
                services.AddScoped<SyncManager>();
                services.AddScoped<DnsService>();
                services.AddScoped<ServiceControl>();
            });

            builder.ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration);
                logging.AddConsole();
            });

            //var host = builder.Build();
            //host.Run();
            await builder.RunConsoleAsync();
        }
    }
}
