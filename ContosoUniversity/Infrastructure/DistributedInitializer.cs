using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace ContosoUniversity.Infrastructure
{
    /// <summary>
    /// Replaces instance-specific Application_Start initialization with:
    ///  1. Centralised configuration loaded from AWS Systems Manager Parameter Store, and
    ///  2. A DynamoDB-based distributed lock so that cluster-wide one-time initialization
    ///     runs exactly once across all scaled instances.
    ///
    /// Environment variables:
    ///   AWS_REGION / AWS_DEFAULT_REGION   AWS region
    ///   APP_CONFIG_PARAMETER_PATH         Parameter Store path prefix (e.g. /contosouniversity/prod/)
    ///   APP_INIT_LOCK_TABLE               DynamoDB table (partition key "LockId", string; TTL attribute "ExpiresAt")
    ///   APP_INSTANCE_ID                   Optional owner id (defaults to machine name + guid)
    ///
    /// When the variables are not set (e.g. local development) the initializer degrades gracefully.
    /// </summary>
    public static class DistributedInitializer
    {
        private static readonly ConcurrentDictionary<string, string> Parameters =
            new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static readonly string OwnerId =
            Environment.GetEnvironmentVariable("APP_INSTANCE_ID") ?? (Environment.MachineName + "-" + Guid.NewGuid().ToString("N"));

        private static RegionEndpoint GetRegion()
        {
            var region = Environment.GetEnvironmentVariable("AWS_REGION")
                         ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION");
            return string.IsNullOrEmpty(region) ? null : RegionEndpoint.GetBySystemName(region);
        }

        /// <summary>Returns a configuration value loaded from Parameter Store (or null).</summary>
        public static string GetParameter(string name)
        {
            string value;
            return Parameters.TryGetValue(name, out value) ? value : null;
        }

        /// <summary>Loads all parameters beneath APP_CONFIG_PARAMETER_PATH from Parameter Store.</summary>
        public static void LoadConfiguration()
        {
            var path = Environment.GetEnvironmentVariable("APP_CONFIG_PARAMETER_PATH");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var region = GetRegion();
            using (var ssm = region == null ? new AmazonSimpleSystemsManagementClient() : new AmazonSimpleSystemsManagementClient(region))
            {
                string nextToken = null;
                do
                {
                    var request = new GetParametersByPathRequest
                    {
                        Path = path,
                        Recursive = true,
                        WithDecryption = true,
                        NextToken = nextToken
                    };
                    var response = ssm.GetParametersByPathAsync(request).GetAwaiter().GetResult();
                    foreach (var p in response.Parameters)
                    {
                        var key = p.Name.StartsWith(path, StringComparison.OrdinalIgnoreCase)
                            ? p.Name.Substring(path.Length).TrimStart('/')
                            : p.Name;
                        Parameters[key] = p.Value;
                    }
                    nextToken = response.NextToken;
                } while (!string.IsNullOrEmpty(nextToken));
            }
        }

        /// <summary>
        /// Runs <paramref name="initialization"/> once across the whole fleet. The instance that
        /// acquires the DynamoDB lock runs it; others wait (bounded) for completion and continue.
        /// If no lock table is configured the action simply runs locally.
        /// </summary>
        public static void RunOnce(string initializationName, Action initialization, TimeSpan? lockTtl = null, TimeSpan? maxWait = null)
        {
            var table = Environment.GetEnvironmentVariable("APP_INIT_LOCK_TABLE");
            if (string.IsNullOrEmpty(table))
            {
                initialization();
                return;
            }

            var ttl = lockTtl ?? TimeSpan.FromMinutes(5);
            var wait = maxWait ?? TimeSpan.FromMinutes(2);
            var lockId = "init#" + initializationName;
            var region = GetRegion();

            using (var ddb = region == null ? new AmazonDynamoDBClient() : new AmazonDynamoDBClient(region))
            {
                var deadline = DateTime.UtcNow + wait;
                while (true)
                {
                    var state = TryAcquire(ddb, table, lockId, ttl);
                    if (state == LockState.Acquired)
                    {
                        try
                        {
                            initialization();
                            MarkCompleted(ddb, table, lockId);
                        }
                        catch
                        {
                            Release(ddb, table, lockId);
                            throw;
                        }
                        return;
                    }

                    if (state == LockState.AlreadyCompleted)
                    {
                        return;
                    }

                    if (DateTime.UtcNow >= deadline)
                    {
                        // Another instance still holds the lock; do not block startup indefinitely.
                        return;
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                }
            }
        }

        private enum LockState { Acquired, Held, AlreadyCompleted }

        private static LockState TryAcquire(IAmazonDynamoDB ddb, string table, string lockId, TimeSpan ttl)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var expires = DateTimeOffset.UtcNow.Add(ttl).ToUnixTimeSeconds();
            try
            {
                ddb.PutItemAsync(new PutItemRequest
                {
                    TableName = table,
                    Item = new Dictionary<string, AttributeValue>
                    {
                        { "LockId", new AttributeValue { S = lockId } },
                        { "Owner", new AttributeValue { S = OwnerId } },
                        { "Status", new AttributeValue { S = "IN_PROGRESS" } },
                        { "ExpiresAt", new AttributeValue { N = expires.ToString(CultureInfo.InvariantCulture) } }
                    },
                    // Acquire when no item exists or a previous in-progress lock has expired.
                    ConditionExpression = "attribute_not_exists(LockId) OR (#s <> :done AND ExpiresAt < :now)",
                    ExpressionAttributeNames = new Dictionary<string, string> { { "#s", "Status" } },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":done", new AttributeValue { S = "COMPLETED" } },
                        { ":now", new AttributeValue { N = now.ToString(CultureInfo.InvariantCulture) } }
                    }
                }).GetAwaiter().GetResult();
                return LockState.Acquired;
            }
            catch (ConditionalCheckFailedException)
            {
                var item = ddb.GetItemAsync(table, new Dictionary<string, AttributeValue> { { "LockId", new AttributeValue { S = lockId } } })
                    .GetAwaiter().GetResult().Item;
                AttributeValue status;
                if (item != null && item.TryGetValue("Status", out status) && status.S == "COMPLETED")
                {
                    return LockState.AlreadyCompleted;
                }
                return LockState.Held;
            }
        }

        private static void MarkCompleted(IAmazonDynamoDB ddb, string table, string lockId)
        {
            ddb.PutItemAsync(new PutItemRequest
            {
                TableName = table,
                Item = new Dictionary<string, AttributeValue>
                {
                    { "LockId", new AttributeValue { S = lockId } },
                    { "Owner", new AttributeValue { S = OwnerId } },
                    { "Status", new AttributeValue { S = "COMPLETED" } }
                }
            }).GetAwaiter().GetResult();
        }

        private static void Release(IAmazonDynamoDB ddb, string table, string lockId)
        {
            try
            {
                ddb.DeleteItemAsync(table, new Dictionary<string, AttributeValue> { { "LockId", new AttributeValue { S = lockId } } })
                    .GetAwaiter().GetResult();
            }
            catch
            {
                // Lock expires via ExpiresAt; nothing further to do.
            }
        }
    }
}
