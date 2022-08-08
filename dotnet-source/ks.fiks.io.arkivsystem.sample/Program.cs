using System;
using System.IO;
using System.Threading.Tasks;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Sok;
using ks.fiks.io.arkivintegrasjon.common.AppSettings;
using ks.fiks.io.arkivsystem.sample.Generators;
using ks.fiks.io.arkivsystem.sample.Handlers;
using ks.fiks.io.arkivsystem.sample.Helpers;
using ks.fiks.io.arkivsystem.sample.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace ks.fiks.io.arkivsystem.sample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            InitSerilogConfiguration();
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
                    services.AddHostedService<ArkivSimulator>()
                        .AddScoped<IArkivmeldingCache, ArkivmeldingCache>()
                        .AddScoped<JournalpostHentHandler, JournalpostHentHandler>()
                        .AddScoped<MappeHentHandler, MappeHentHandler>()
                        .AddScoped<ArkivmeldingHandler, ArkivmeldingHandler>()
                        .AddScoped<ArkivmeldingOppdaterHandler, ArkivmeldingOppdaterHandler>()
                        .AddScoped<SokHandler, SokHandler>()
                        .AddScoped<SokGenerator, SokGenerator>()
                        .AddScoped<SokeresultatGenerator, SokeresultatGenerator>();
                })
                .RunConsoleAsync();
        }

        private static void InitSerilogConfiguration()
        {
            var aspnetcoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var logstashDestination = Environment.GetEnvironmentVariable("LOGSTASH_DESTINATION");
            var hostname = Environment.GetEnvironmentVariable("HOSTNAME");
            var kubernetesNode = Environment.GetEnvironmentVariable("KUBERNETES_NODE");
            var environment = Environment.GetEnvironmentVariable("ENVIRONMENT");
            
            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Localization", LogEventLevel.Error)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("app", "arkivsystem-simulator")
                .Enrich.WithProperty("env", environment)
                .Enrich.WithProperty("logsource", hostname)
                .Enrich.WithProperty("node", kubernetesNode)
                .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level}] [{RequestId}] [{requestid}] - {Message} {NewLine} {Exception}");

            Log.Logger = loggerConfiguration.CreateLogger();

            Log.Information("Starting host with env variables:");
            Log.Information("ASPNETCORE_ENVIRONMENT: {AspnetcoreEnvironment}", aspnetcoreEnvironment);
            Log.Information("HOSTNAME: {Hostname}", hostname);
            Log.Information("KUBERNETES_NODE: {KubernetesNode}", kubernetesNode);
            Log.Information("ENVIRONMENT: {Environment}",environment);
            Log.Information("LOGSTASH_DESTINATION: {LogstashDestination}", logstashDestination);
            Log.Information("Path.PathSeparator: {PathSeparator}", Path.PathSeparator);
        }
    }
}
