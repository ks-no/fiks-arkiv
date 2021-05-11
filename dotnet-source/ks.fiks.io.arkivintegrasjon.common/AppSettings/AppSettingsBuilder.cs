using System;
using Microsoft.Extensions.Configuration;

namespace ks.fiks.io.arkivintegrasjon.common.AppSettings
{
    public static class AppSettingsBuilder
    {
        public static AppSettings CreateAppSettings(IConfiguration configuration)
        {
            var appSettings = configuration.GetSection("AppSettings").Get<AppSettings>();
            
            // Available from maskinporten-config config-map in k8s cluster
            var maskinportenClientId = Environment.GetEnvironmentVariable("MASKINPORTEN_CLIENT_ID");
            
            if (!string.IsNullOrEmpty(maskinportenClientId))
            {
                appSettings.FiksIOConfig.MaskinPortenIssuer = maskinportenClientId;
            }
            
            return appSettings;
        }
    }
}