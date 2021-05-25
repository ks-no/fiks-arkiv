using System.IO;
using System.Threading.Tasks;
using ks.fiks.io.arkivintegrasjon.common.AppSettings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ks.fiks.io.arkivsystem.sample
{
    class Program
    {
        static async Task Main(string[] args)
        { 
            await new HostBuilder()
                .ConfigureHostConfiguration((configHost) =>
                {
                    configHost.AddEnvironmentVariables("DOTNET_");
                })
                .ConfigureAppConfiguration((hostBuilder, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appsettings.json", optional: true);
                    config.AddJsonFile($"appsettings.{hostBuilder.HostingEnvironment.EnvironmentName}.json", optional: true);
                    config.AddEnvironmentVariables("fiksArkivMock_");
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton(AppSettingsBuilder.CreateAppSettings(hostContext.Configuration));
                    services.AddHostedService<ArkivService>();
                })
                .RunConsoleAsync();
        }
    }
}
