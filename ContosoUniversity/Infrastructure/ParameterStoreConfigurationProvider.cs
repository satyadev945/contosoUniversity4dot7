using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace ContosoUniversity.Infrastructure
{
    /// <summary>
    /// Loads centralized application configuration from AWS Systems Manager Parameter Store so that every
    /// scaled instance starts with identical settings (instead of per-instance Global.asax state).
    /// The parameter hierarchy is supplied through the SSM_PARAMETER_PATH environment variable
    /// (for example "/contosouniversity/prod/"). Values are exposed through the process environment so that
    /// the rest of the application can keep reading them via Environment.GetEnvironmentVariable.
    /// Explicitly set environment variables always take precedence over Parameter Store values.
    /// </summary>
    public static class ParameterStoreConfigurationProvider
    {
        public const string PathVariable = "SSM_PARAMETER_PATH";

        public static IDictionary<string, string> Load()
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var path = Environment.GetEnvironmentVariable(PathVariable);
            if (string.IsNullOrWhiteSpace(path))
            {
                return values; // Parameter Store not configured (e.g. local development)
            }

            if (!path.EndsWith("/")) path += "/";

            try
            {
                using (var client = new AmazonSimpleSystemsManagementClient())
                {
                    string nextToken = null;
                    do
                    {
                        var response = client.GetParametersByPathAsync(new GetParametersByPathRequest
                        {
                            Path = path,
                            Recursive = true,
                            WithDecryption = true,
                            NextToken = nextToken
                        }).GetAwaiter().GetResult();

                        foreach (var p in response.Parameters)
                        {
                            var key = p.Name.Substring(path.Length).Replace('/', ':');
                            values[key] = p.Value;
                        }
                        nextToken = response.NextToken;
                    } while (!string.IsNullOrEmpty(nextToken));
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("{\"level\":\"Warning\",\"message\":\"Unable to load AWS Parameter Store configuration\",\"path\":\"" + path + "\",\"error\":\"" + ex.Message.Replace("\"", "'") + "\"}");
            }

            return values;
        }

        /// <summary>
        /// Publishes Parameter Store values as environment variables (without overriding existing ones).
        /// Hierarchy separators are mapped to "__" (e.g. ConnectionStrings/SchoolContext -> ConnectionStrings__SchoolContext).
        /// </summary>
        public static void ApplyToEnvironment()
        {
            foreach (var kvp in Load())
            {
                var name = kvp.Key.Replace(":", "__");
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name)))
                {
                    Environment.SetEnvironmentVariable(name, kvp.Value);
                }
            }
        }
    }
}
