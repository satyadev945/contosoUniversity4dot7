using System;
using System.Collections.Concurrent;
using System.Configuration;
using Azure.Data.AppConfiguration;
using Azure.Identity;

namespace ContosoUniversity.Hosting
{
    /// <summary>
    /// Centralized configuration backed by Azure App Configuration so that every scaled
    /// Azure App Service instance initializes with identical settings.
    /// Configuration (environment variables take precedence over appSettings):
    ///   APP_CONFIGURATION_ENDPOINT   e.g. https://my-config.azconfig.io  (uses DefaultAzureCredential / managed identity)
    ///   APP_CONFIGURATION_CONNECTION_STRING   optional alternative to the endpoint
    ///   APP_CONFIGURATION_LABEL      optional label filter (e.g. Production)
    /// Lookup order: Azure App Configuration -> environment variable -> appSettings.
    /// </summary>
    public static class AzureAppConfigurationSettings
    {
        private static readonly ConcurrentDictionary<string, string> Values =
            new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static bool IsConfigured
        {
            get
            {
                return !string.IsNullOrWhiteSpace(GetLocal("APP_CONFIGURATION_ENDPOINT"))
                    || !string.IsNullOrWhiteSpace(GetLocal("APP_CONFIGURATION_CONNECTION_STRING"));
            }
        }

        public static string Get(string name)
        {
            string value;
            if (Values.TryGetValue(name, out value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
            return GetLocal(name);
        }

        /// <summary>Loads all key-values (optionally filtered by label) from Azure App Configuration.</summary>
        public static void Load()
        {
            if (!IsConfigured)
            {
                return;
            }

            var connectionString = GetLocal("APP_CONFIGURATION_CONNECTION_STRING");
            ConfigurationClient client = !string.IsNullOrWhiteSpace(connectionString)
                ? new ConfigurationClient(connectionString)
                : new ConfigurationClient(new Uri(GetLocal("APP_CONFIGURATION_ENDPOINT")), new DefaultAzureCredential());

            var label = GetLocal("APP_CONFIGURATION_LABEL");
            var selector = new SettingSelector { LabelFilter = string.IsNullOrWhiteSpace(label) ? null : label };

            foreach (var setting in client.GetConfigurationSettings(selector))
            {
                Values[setting.Key] = setting.Value;
            }
        }

        private static string GetLocal(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(value) ? ConfigurationManager.AppSettings[name] : value;
        }
    }
}
