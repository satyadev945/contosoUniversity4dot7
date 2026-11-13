using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace ContosoUniversity.Infrastructure
{
    /// <summary>
    /// Replaces instance-specific Global.asax Application_Start initialization.
    ///  - Centralized configuration is read from AWS Systems Manager Parameter Store
    ///    (path prefix from APP_CONFIG_PARAMETER_PATH, e.g. /contoso-university/prod/).
    ///  - Cluster-wide one-time initialization is coordinated with a DynamoDB conditional-write lock
    ///    (table name from INIT_LOCK_TABLE, partition key "LockId" (S)), so exactly one scaled instance
    ///    runs it while the others wait for/skip it.
    /// Per-process setup (e.g. EF interceptors, middleware) stays in Startup because it must run on every instance.
    /// If the environment variables are not set (e.g. local development) the initializer is a no-op.
    /// </summary>
    public static class DistributedInitializer
    {
        public const string ParameterPathEnvironmentVariable = "APP_CONFIG_PARAMETER_PATH";
        public const string LockTableEnvironmentVariable = "INIT_LOCK_TABLE";
        public const string LockTtlSecondsEnvironmentVariable = "INIT_LOCK_TTL_SECONDS";

        /// <summary>Loaded Parameter Store values (key = parameter name relative to the path prefix).</summary>
        public static IReadOnlyDictionary<string, string> Configuration { get; private set; } =
            new Dictionary<string, string>();

        public static void Initialize(string initializationName, Action oneTimeInitialization)
        {
            InitializeAsync(initializationName, oneTimeInitialization).GetAwaiter().GetResult();
        }

        public static async Task InitializeAsync(string initializationName, Action oneTimeInitialization)
        {
            await LoadParametersAsync().ConfigureAwait(false);

            var table = Environment.GetEnvironmentVariable(LockTableEnvironmentVariable);
            if (string.IsNullOrEmpty(table))
            {
                oneTimeInitialization?.Invoke();
                return;
            }

            var ttl = 300;
            int parsed;
            if (int.TryParse(Environment.GetEnvironmentVariable(LockTtlSecondsEnvironmentVariable), out parsed) && parsed > 0)
            {
                ttl = parsed;
            }

            var owner = Environment.MachineName + ":" + Guid.NewGuid().ToString("N");
            using (var dynamo = new AmazonDynamoDBClient())
            {
                var lockId = "init:" + initializationName;
                var deadline = DateTime.UtcNow.AddSeconds(ttl);
                while (DateTime.UtcNow < deadline)
                {
                    var state = await TryAcquireAsync(dynamo, table, lockId, owner, ttl).ConfigureAwait(false);
                    if (state == LockState.AlreadyCompleted)
                    {
                        return; // another instance already initialized the cluster
                    }
                    if (state == LockState.Acquired)
                    {
                        try
                        {
                            oneTimeInitialization?.Invoke();
                            await MarkCompletedAsync(dynamo, table, lockId, owner).ConfigureAwait(false);
                        }
                        catch
                        {
                            await ReleaseAsync(dynamo, table, lockId, owner).ConfigureAwait(false);
                            throw;
                        }
                        return;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                }
                throw new TimeoutException("Timed out waiting for distributed initialization '" + initializationName + "'.");
            }
        }

        private enum LockState { Acquired, Held, AlreadyCompleted }

        private static async Task<LockState> TryAcquireAsync(IAmazonDynamoDB dynamo, string table, string lockId, string owner, int ttlSeconds)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            try
            {
                await dynamo.PutItemAsync(new PutItemRequest
                {
                    TableName = table,
                    Item = new Dictionary<string, AttributeValue>
                    {
                        { "LockId", new AttributeValue { S = lockId } },
                        { "Owner", new AttributeValue { S = owner } },
                        { "Status", new AttributeValue { S = "IN_PROGRESS" } },
                        { "ExpiresAt", new AttributeValue { N = (now + ttlSeconds).ToString() } }
                    },
                    // acquire if absent, or if a previous holder's lease expired without completing
                    ConditionExpression = "attribute_not_exists(LockId) OR (#s <> :done AND ExpiresAt < :now)",
                    ExpressionAttributeNames = new Dictionary<string, string> { { "#s", "Status" } },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":done", new AttributeValue { S = "COMPLETED" } },
                        { ":now", new AttributeValue { N = now.ToString() } }
                    }
                }).ConfigureAwait(false);
                return LockState.Acquired;
            }
            catch (ConditionalCheckFailedException)
            {
                var existing = await dynamo.GetItemAsync(new GetItemRequest
                {
                    TableName = table,
                    ConsistentRead = true,
                    Key = new Dictionary<string, AttributeValue> { { "LockId", new AttributeValue { S = lockId } } }
                }).ConfigureAwait(false);
                AttributeValue status;
                if (existing.Item != null && existing.Item.TryGetValue("Status", out status) && status.S == "COMPLETED")
                {
                    return LockState.AlreadyCompleted;
                }
                return LockState.Held;
            }
        }

        private static Task MarkCompletedAsync(IAmazonDynamoDB dynamo, string table, string lockId, string owner)
        {
            return dynamo.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = table,
                Key = new Dictionary<string, AttributeValue> { { "LockId", new AttributeValue { S = lockId } } },
                UpdateExpression = "SET #s = :done REMOVE ExpiresAt",
                ConditionExpression = "Owner = :owner",
                ExpressionAttributeNames = new Dictionary<string, string> { { "#s", "Status" } },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":done", new AttributeValue { S = "COMPLETED" } },
                    { ":owner", new AttributeValue { S = owner } }
                }
            });
        }

        private static async Task ReleaseAsync(IAmazonDynamoDB dynamo, string table, string lockId, string owner)
        {
            try
            {
                await dynamo.DeleteItemAsync(new DeleteItemRequest
                {
                    TableName = table,
                    Key = new Dictionary<string, AttributeValue> { { "LockId", new AttributeValue { S = lockId } } },
                    ConditionExpression = "Owner = :owner",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":owner", new AttributeValue { S = owner } }
                    }
                }).ConfigureAwait(false);
            }
            catch (ConditionalCheckFailedException)
            {
                // lock no longer ours; nothing to release
            }
        }

        private static async Task LoadParametersAsync()
        {
            var path = Environment.GetEnvironmentVariable(ParameterPathEnvironmentVariable);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var ssm = new AmazonSimpleSystemsManagementClient())
            {
                string nextToken = null;
                do
                {
                    var response = await ssm.GetParametersByPathAsync(new GetParametersByPathRequest
                    {
                        Path = path,
                        Recursive = true,
                        WithDecryption = true,
                        NextToken = nextToken
                    }).ConfigureAwait(false);

                    foreach (var p in response.Parameters)
                    {
                        values[p.Name.StartsWith(path, StringComparison.Ordinal) ? p.Name.Substring(path.Length).TrimStart('/') : p.Name] = p.Value;
                    }
                    nextToken = response.NextToken;
                } while (!string.IsNullOrEmpty(nextToken));
            }
            Configuration = values;
        }
    }
}
