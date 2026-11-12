using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Microsoft.Extensions.Configuration;

namespace ContosoUniversity.Infrastructure
{
    /// <summary>
    /// Loads centralized application settings from AWS Systems Manager Parameter Store so every
    /// scaled instance starts with identical configuration (replaces per-instance Global.asax setup).
    /// The path is taken from the APP_PARAMETER_PATH environment variable (e.g. /contosouniversity/prod/).
    /// When the variable is not set the provider is a no-op (local development).
    /// </summary>
    public class ParameterStoreConfigurationSource : IConfigurationSource
    {
        public string Path { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new ParameterStoreConfigurationProvider(Path);
        }
    }

    public class ParameterStoreConfigurationProvider : ConfigurationProvider
    {
        private readonly string _path;

        public ParameterStoreConfigurationProvider(string path)
        {
            _path = path;
        }

        public override void Load()
        {
            if (string.IsNullOrWhiteSpace(_path))
            {
                return;
            }

            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var client = new AmazonSimpleSystemsManagementClient())
            {
                string nextToken = null;
                do
                {
                    var request = new GetParametersByPathRequest
                    {
                        Path = _path,
                        Recursive = true,
                        WithDecryption = true,
                        NextToken = nextToken
                    };
                    var response = Task.Run(() => client.GetParametersByPathAsync(request)).GetAwaiter().GetResult();
                    foreach (var p in response.Parameters)
                    {
                        var key = p.Name.Substring(_path.Length).Trim('/').Replace("/", ":");
                        data[key] = p.Value;
                    }
                    nextToken = response.NextToken;
                } while (!string.IsNullOrEmpty(nextToken));
            }

            Data = data;
        }
    }

    public static class ParameterStoreConfigurationExtensions
    {
        public static IConfigurationBuilder AddAwsParameterStore(this IConfigurationBuilder builder, string path)
        {
            return builder.Add(new ParameterStoreConfigurationSource { Path = path });
        }
    }
}
