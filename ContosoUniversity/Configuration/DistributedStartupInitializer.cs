using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace ContosoUniversity.Configuration
{
    /// <summary>
    /// Replaces the instance-specific initialization that used to live in Global.asax
    /// Application_Start. Every scaled instance now:
    ///   1. loads shared configuration from AWS Systems Manager Parameter Store
    ///      (path given by the APP_CONFIG_PARAMETER_PATH environment variable), and
    ///   2. coordinates one-time, cluster-wide initialization through a DynamoDB
    ///      conditional-write lock (table given by the INIT_LOCK_TABLE environment variable;
    ///      partition key "LockId" of type String, optional TTL attribute "ExpiresAt").
    /// When the environment variables are not set (e.g. local development) the initializer
    /// degrades gracefully: no parameters are loaded and the action simply runs locally.
    /// </summary>
    public static class DistributedStartupInitializer
    {
        private const string ParameterPathVariable = "APP_CONFIG_PARAMETER_PATH";
        private const string LockTableVariable = "INIT_LOCK_TABLE";
        private const string LockIdAttribute = "LockId";
        private const string ExpiresAtAttribute = "ExpiresAt";
        private const string StateAttribute = "State";
        private const string OwnerAttribute = "Owner";
        private const string StateInProgress = "IN_PROGRESS";
        private const string StateCompleted = "COMPLETED";

        private static readonly TimeSpan LockLease = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan WaitTimeout = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
        private static readonly string InstanceId = Environment.MachineName + ":" + Guid.NewGuid().ToString("N");

        private static readonly object Sync = new object();
        private static IDictionary<string, string> _parameters = new Dictionary<string, string>();

        /// <summary>Configuration values loaded from Parameter Store (keys are relative to the configured path).</summary>
        public static string GetParameter(string name)
        {
            lock (Sync)
            {
                string value;
                return _parameters.TryGetValue(name, out value) ? value : null;
            }
        }

        /// <summary>
        /// Loads centralized configuration from AWS Systems Manager Parameter Store.
        /// Values already supplied through environment variables are not overridden.
        /// </summary>
        public static void LoadSharedConfiguration()
        {
            var path = Environment.GetEnvironmentVariable(ParameterPathVariable);
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                var loaded = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                using (var ssm = CreateClient<AmazonSimpleSystemsManagementClient>(
                    r => new AmazonSimpleSystemsManagementClient(r), () => new AmazonSimpleSystemsManagementClient()))
                {
                    string nextToken = null;
                    do
                    {
                        var response = ssm.GetParametersByPath(new GetParametersByPathRequest
                        {
                            Path = path,
                            Recursive = true,
                            WithDecryption = true,
                            NextToken = nextToken
                        });

                        foreach (var p in response.Parameters)
                        {
                            var key = p.Name.StartsWith(path, StringComparison.OrdinalIgnoreCase)
                                ? p.Name.Substring(path.Length).TrimStart('/')
                                : p.Name;
                            loaded[key] = p.Value;

                            // Expose as "A:B" style key -> environment variable, without overriding explicit settings.
                            var envName = key.Replace('/', '_').Replace(':', '_').ToUpperInvariant();
                            if (Environment.GetEnvironmentVariable(envName) == null)
                            {
                                Environment.SetEnvironmentVariable(envName, p.Value);
                            }
                        }

                        nextToken = response.NextToken;
                    } while (!string.IsNullOrEmpty(nextToken));
                }

                lock (Sync)
                {
                    _parameters = loaded;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Unable to load configuration from AWS Parameter Store path '" + path + "': " + ex.Message);
            }
        }

        /// <summary>
        /// Runs <paramref name="initialization"/> exactly once across all instances for the given
        /// initialization name/version. Other instances wait until the leader has completed.
        /// </summary>
        public static void RunOnce(string initializationName, Action initialization)
        {
            if (initialization == null) throw new ArgumentNullException("initialization");

            var table = Environment.GetEnvironmentVariable(LockTableVariable);
            if (string.IsNullOrWhiteSpace(table))
            {
                // No distributed lock configured (local development): run locally.
                initialization();
                return;
            }

            var lockId = "init#" + initializationName + "#" +
                         (Environment.GetEnvironmentVariable("APP_VERSION") ?? "default");

            using (var dynamo = CreateClient<AmazonDynamoDBClient>(
                r => new AmazonDynamoDBClient(r), () => new AmazonDynamoDBClient()))
            {
                if (TryAcquire(dynamo, table, lockId))
                {
                    try
                    {
                        initialization();
                        MarkCompleted(dynamo, table, lockId);
                    }
                    catch
                    {
                        Release(dynamo, table, lockId);
                        throw;
                    }
                    return;
                }

                WaitForCompletion(dynamo, table, lockId);
            }
        }

        private static bool TryAcquire(IAmazonDynamoDB dynamo, string table, string lockId)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            try
            {
                dynamo.PutItem(new PutItemRequest
                {
                    TableName = table,
                    Item = new Dictionary<string, AttributeValue>
                    {
                        { LockIdAttribute, new AttributeValue { S = lockId } },
                        { StateAttribute, new AttributeValue { S = StateInProgress } },
                        { OwnerAttribute, new AttributeValue { S = InstanceId } },
                        { ExpiresAtAttribute, new AttributeValue { N = (now + (long)LockLease.TotalSeconds).ToString() } }
                    },
                    // Acquire when nobody holds the lock, or a previous in-progress lease has expired.
                    ConditionExpression = "attribute_not_exists(#id) OR (#state = :inprogress AND #exp < :now)",
                    ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        { "#id", LockIdAttribute },
                        { "#state", StateAttribute },
                        { "#exp", ExpiresAtAttribute }
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":inprogress", new AttributeValue { S = StateInProgress } },
                        { ":now", new AttributeValue { N = now.ToString() } }
                    }
                });
                return true;
            }
            catch (ConditionalCheckFailedException)
            {
                return false;
            }
        }

        private static void MarkCompleted(IAmazonDynamoDB dynamo, string table, string lockId)
        {
            dynamo.PutItem(new PutItemRequest
            {
                TableName = table,
                Item = new Dictionary<string, AttributeValue>
                {
                    { LockIdAttribute, new AttributeValue { S = lockId } },
                    { StateAttribute, new AttributeValue { S = StateCompleted } },
                    { OwnerAttribute, new AttributeValue { S = InstanceId } }
                }
            });
        }

        private static void Release(IAmazonDynamoDB dynamo, string table, string lockId)
        {
            try
            {
                dynamo.DeleteItem(new DeleteItemRequest
                {
                    TableName = table,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        { LockIdAttribute, new AttributeValue { S = lockId } }
                    }
                });
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Unable to release initialization lock '" + lockId + "': " + ex.Message);
            }
        }

        private static void WaitForCompletion(IAmazonDynamoDB dynamo, string table, string lockId)
        {
            var deadline = DateTime.UtcNow + WaitTimeout;
            while (DateTime.UtcNow < deadline)
            {
                var item = dynamo.GetItem(new GetItemRequest
                {
                    TableName = table,
                    ConsistentRead = true,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        { LockIdAttribute, new AttributeValue { S = lockId } }
                    }
                }).Item;

                if (item != null && item.Count > 0)
                {
                    AttributeValue state;
                    if (item.TryGetValue(StateAttribute, out state) && state.S == StateCompleted)
                    {
                        return;
                    }
                }

                Thread.Sleep(PollInterval);
            }

            Trace.TraceWarning("Timed out waiting for distributed initialization '" + lockId + "' to complete.");
        }

        private static T CreateClient<T>(Func<RegionEndpoint, T> withRegion, Func<T> defaultClient)
        {
            var region = Environment.GetEnvironmentVariable("AWS_REGION")
                         ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION");
            return string.IsNullOrWhiteSpace(region)
                ? defaultClient()
                : withRegion(RegionEndpoint.GetBySystemName(region));
        }
    }
}
