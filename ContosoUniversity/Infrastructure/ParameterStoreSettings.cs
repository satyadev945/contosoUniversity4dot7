using System;
using System.Collections.Generic;
using System.Diagnostics;
using Amazon;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace ContosoUniversity.Infrastructure
{
    /// <summary>
    /// Centralized application settings backed by AWS Systems Manager Parameter Store.
    /// Every scaled instance reads the same parameters (hierarchy configured through the
    /// PARAMETER_STORE_PATH environment variable, e.g. "/contoso-university/prod/"), so no
    /// instance-specific state is created during application startup.
    /// Lookup order for <see cref="Get"/>: Parameter Store, then environment variable, then default.
    /// </summary>
    public static class ParameterStoreSettings
    {
        public const string PathEnvVar = "PARAMETER_STORE_PATH";

        private static readonly object SyncRoot = new object();
        private static Dictionary<string, string> _values =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Loads (or reloads) all parameters under the configured path. Safe to call from every instance.</summary>
        public static void Load()
        {
            var path = Environment.GetEnvironmentVariable(PathEnvVar);
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }
            if (!path.EndsWith("/", StringComparison.Ordinal))
            {
                path += "/";
            }

            var loaded = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var client = CreateClient())
                {
                    string nextToken = null;
                    do
                    {
                        var response = client.GetParametersByPath(new GetParametersByPathRequest
                        {
                            Path = path,
                            Recursive = true,
                            WithDecryption = true,
                            NextToken = nextToken
                        });
                        foreach (var p in response.Parameters)
                        {
                            loaded[p.Name.Substring(path.Length)] = p.Value;
                        }
                        nextToken = response.NextToken;
                    } while (!string.IsNullOrEmpty(nextToken));
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Unable to load configuration from AWS Parameter Store path '" + path + "': " + ex.Message);
                return;
            }

            lock (SyncRoot)
            {
                _values = loaded;
            }
        }

        public static string Get(string key, string defaultValue = null)
        {
            lock (SyncRoot)
            {
                string value;
                if (_values.TryGetValue(key, out value) && !string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
            var env = Environment.GetEnvironmentVariable(key);
            return string.IsNullOrEmpty(env) ? defaultValue : env;
        }

        private static AmazonSimpleSystemsManagementClient CreateClient()
        {
            var region = Environment.GetEnvironmentVariable("AWS_REGION")
                         ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION");
            return string.IsNullOrWhiteSpace(region)
                ? new AmazonSimpleSystemsManagementClient()
                : new AmazonSimpleSystemsManagementClient(RegionEndpoint.GetBySystemName(region));
        }
    }
}
