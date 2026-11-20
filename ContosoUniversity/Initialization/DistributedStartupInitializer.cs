using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using ContosoUniversity.DAL;
using System.Data.Entity.Infrastructure.Interception;

namespace ContosoUniversity.Initialization
{
    /// <summary>
    /// Replaces the per-instance Global.asax Application_Start initialisation.
    ///  - Configuration is read centrally from AWS Systems Manager Parameter Store
    ///    (prefix from STARTUP_PARAMETER_PREFIX, default "/contosouniversity/").
    ///  - Cluster-wide one-time initialisation is coordinated with a DynamoDB lock
    ///    (table from STARTUP_LOCK_TABLE; partition key "LockId" (S); optional TTL attribute "ExpiresAt")
    ///    so that exactly one instance runs it while the others wait for / observe completion.
    ///  - Process-local hooks (EF6 DbInterception is process-wide state) are registered on every instance,
    ///    driven by the same centrally managed parameters so all instances are configured consistently.
    /// If the AWS settings are not provided the initializer falls back to the previous behaviour.
    /// </summary>
    public static class DistributedStartupInitializer
    {
        public const string ParameterPrefixEnvVar = "STARTUP_PARAMETER_PREFIX";
        public const string LockTableEnvVar = "STARTUP_LOCK_TABLE";
        private const string LockId = "contosouniversity-startup-init";
        private static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(60);

        private static int _initialized;

        public static void Initialize()
        {
            if (Interlocked.Exchange(ref _initialized, 1) == 1)
            {
                return;
            }

            var parameters = LoadParameters();

            // Per-process hooks: must run on every instance, configuration comes from Parameter Store.
            DbInterception.Add(new SchoolInterceptorTransientErrors());
            if (GetBool(parameters, "db-logging-enabled", true))
            {
                DbInterception.Add(new SchoolInterceptorLogging());
            }

            // Cluster-wide one-time initialisation guarded by a distributed lock.
            RunOncePerCluster(() =>
            {
                Log("info", "Cluster-wide startup initialisation executed by this instance");
            });
        }

        private static string Prefix
        {
            get
            {
                var p = Environment.GetEnvironmentVariable(ParameterPrefixEnvVar);
                if (string.IsNullOrWhiteSpace(p)) p = "/contosouniversity/";
                return p.EndsWith("/") ? p : p + "/";
            }
        }

        private static RegionEndpoint Region
        {
            get
            {
                var name = Environment.GetEnvironmentVariable("AWS_REGION")
                           ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION");
                return string.IsNullOrWhiteSpace(name) ? null : RegionEndpoint.GetBySystemName(name);
            }
        }

        private static Dictionary<string, string> LoadParameters()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ParameterPrefixEnvVar)))
            {
                return result;
            }

            try
            {
                var region = Region;
                using (var client = region == null ? new AmazonSimpleSystemsManagementClient() : new AmazonSimpleSystemsManagementClient(region))
                {
                    string next = null;
                    do
                    {
                        var response = client.GetParametersByPathAsync(new GetParametersByPathRequest
                        {
                            Path = Prefix,
                            Recursive = true,
                            WithDecryption = true,
                            NextToken = next
                        }).GetAwaiter().GetResult();
                        foreach (var p in response.Parameters)
                        {
                            result[p.Name.Substring(Prefix.Length)] = p.Value;
                        }
                        next = response.NextToken;
                    } while (!string.IsNullOrEmpty(next));
                }
            }
            catch (Exception ex)
            {
                Log("warn", "Unable to load Parameter Store configuration; using defaults (" + ex.GetType().Name + ")");
            }
            return result;
        }

        private static bool GetBool(Dictionary<string, string> p, string key, bool defaultValue)
        {
            string v;
            bool b;
            return p.TryGetValue(key, out v) && bool.TryParse(v, out b) ? b : defaultValue;
        }

        private static void RunOncePerCluster(Action work)
        {
            var table = Environment.GetEnvironmentVariable(LockTableEnvVar);
            if (string.IsNullOrWhiteSpace(table))
            {
                work();
                return;
            }

            var owner = Environment.MachineName + ":" + Guid.NewGuid().ToString("N");
            try
            {
                var region = Region;
                using (var client = region == null ? new AmazonDynamoDBClient() : new AmazonDynamoDBClient(region))
                {
                    var deadline = DateTime.UtcNow + WaitTimeout;
                    while (DateTime.UtcNow < deadline)
                    {
                        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        try
                        {
                            client.PutItemAsync(new PutItemRequest
                            {
                                TableName = table,
                                Item = new Dictionary<string, AttributeValue>
                                {
                                    { "LockId", new AttributeValue { S = LockId } },
                                    { "Owner", new AttributeValue { S = owner } },
                                    { "ExpiresAt", new AttributeValue { N = (now + (long)LockTtl.TotalSeconds).ToString() } }
                                },
                                ConditionExpression = "attribute_not_exists(LockId) OR ExpiresAt < :now",
                                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                                {
                                    { ":now", new AttributeValue { N = now.ToString() } }
                                }
                            }).GetAwaiter().GetResult();
                        }
                        catch (ConditionalCheckFailedException)
                        {
                            // Another instance holds the lock; it will run the initialisation.
                            Thread.Sleep(TimeSpan.FromSeconds(2));
                            return;
                        }

                        try
                        {
                            work();
                        }
                        finally
                        {
                            client.DeleteItemAsync(new DeleteItemRequest
                            {
                                TableName = table,
                                Key = new Dictionary<string, AttributeValue> { { "LockId", new AttributeValue { S = LockId } } },
                                ConditionExpression = "Owner = :o",
                                ExpressionAttributeValues = new Dictionary<string, AttributeValue> { { ":o", new AttributeValue { S = owner } } }
                            }).GetAwaiter().GetResult();
                        }
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("warn", "Distributed lock unavailable (" + ex.GetType().Name + "); continuing startup");
            }
        }

        private static void Log(string level, string message)
        {
            Console.Error.WriteLine("{\"level\":\"" + level + "\",\"message\":\"" + message.Replace("\"", "'") + "\"}");
        }
    }
}
