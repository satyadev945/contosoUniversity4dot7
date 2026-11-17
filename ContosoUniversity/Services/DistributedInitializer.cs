using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// Replaces instance-specific Global.asax Application_Start initialization.
    /// <list type="bullet">
    /// <item>Shared configuration is loaded from AWS Systems Manager Parameter Store
    /// (path given by SSM_PARAMETER_PATH) so every scaled instance gets identical settings.</item>
    /// <item>One-time initialization work (e.g. database initialization/seeding) is guarded by a
    /// distributed lock held in DynamoDB (table given by INIT_LOCK_TABLE, partition key "LockId")
    /// so exactly one instance runs it; the others wait for completion.</item>
    /// </list>
    /// Per-process registrations (middleware, EF interceptors) remain in Startup because they must
    /// execute on every instance. When the env vars are not set, the feature is a no-op.
    /// </summary>
    public class DistributedInitializer
    {
        private const string StatusInProgress = "IN_PROGRESS";
        private const string StatusCompleted = "COMPLETED";

        private readonly string _parameterPath = Environment.GetEnvironmentVariable("SSM_PARAMETER_PATH");
        private readonly string _lockTable = Environment.GetEnvironmentVariable("INIT_LOCK_TABLE");
        private readonly string _instanceId = Environment.MachineName + ":" + Guid.NewGuid().ToString("N");
        private readonly TimeSpan _lockTtl = TimeSpan.FromMinutes(ReadInt("INIT_LOCK_TTL_MINUTES", 5));
        private readonly TimeSpan _waitTimeout = TimeSpan.FromMinutes(ReadInt("INIT_LOCK_WAIT_MINUTES", 5));

        /// <summary>
        /// Loads all parameters under SSM_PARAMETER_PATH into process environment variables
        /// (existing environment variables take precedence).
        /// </summary>
        public async Task LoadSharedConfigurationAsync()
        {
            if (string.IsNullOrWhiteSpace(_parameterPath))
            {
                return;
            }

            using (var ssm = new AmazonSimpleSystemsManagementClient())
            {
                string nextToken = null;
                do
                {
                    var response = await ssm.GetParametersByPathAsync(new GetParametersByPathRequest
                    {
                        Path = _parameterPath,
                        Recursive = true,
                        WithDecryption = true,
                        NextToken = nextToken
                    }).ConfigureAwait(false);

                    foreach (var p in response.Parameters)
                    {
                        var name = p.Name.Substring(p.Name.LastIndexOf('/') + 1);
                        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name)))
                        {
                            Environment.SetEnvironmentVariable(name, p.Value);
                        }
                    }
                    nextToken = response.NextToken;
                } while (!string.IsNullOrEmpty(nextToken));
            }
        }

        /// <summary>
        /// Runs <paramref name="initialization"/> exactly once across all instances.
        /// </summary>
        public async Task RunOnceAsync(string lockId, Func<Task> initialization)
        {
            if (string.IsNullOrWhiteSpace(_lockTable))
            {
                // Distributed coordination not configured; nothing to coordinate.
                return;
            }

            using (var ddb = new AmazonDynamoDBClient())
            {
                var deadline = DateTime.UtcNow + _waitTimeout;
                while (true)
                {
                    if (await IsCompletedAsync(ddb, lockId).ConfigureAwait(false))
                    {
                        return;
                    }

                    if (await TryAcquireAsync(ddb, lockId).ConfigureAwait(false))
                    {
                        try
                        {
                            await initialization().ConfigureAwait(false);
                            await MarkCompletedAsync(ddb, lockId).ConfigureAwait(false);
                        }
                        catch
                        {
                            await ReleaseAsync(ddb, lockId).ConfigureAwait(false);
                            throw;
                        }
                        return;
                    }

                    if (DateTime.UtcNow > deadline)
                    {
                        throw new TimeoutException("Timed out waiting for distributed initialization '" + lockId + "'.");
                    }
                    await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                }
            }
        }

        private async Task<bool> IsCompletedAsync(IAmazonDynamoDB ddb, string lockId)
        {
            var item = await ddb.GetItemAsync(new GetItemRequest
            {
                TableName = _lockTable,
                ConsistentRead = true,
                Key = new Dictionary<string, AttributeValue> { { "LockId", new AttributeValue { S = lockId } } }
            }).ConfigureAwait(false);

            AttributeValue status;
            return item.Item != null && item.Item.TryGetValue("Status", out status) && status.S == StatusCompleted;
        }

        private async Task<bool> TryAcquireAsync(IAmazonDynamoDB ddb, string lockId)
        {
            var now = DateTimeOffset.UtcNow;
            try
            {
                await ddb.PutItemAsync(new PutItemRequest
                {
                    TableName = _lockTable,
                    Item = new Dictionary<string, AttributeValue>
                    {
                        { "LockId", new AttributeValue { S = lockId } },
                        { "Status", new AttributeValue { S = StatusInProgress } },
                        { "Owner", new AttributeValue { S = _instanceId } },
                        { "ExpiresAt", new AttributeValue { N = now.Add(_lockTtl).ToUnixTimeSeconds().ToString() } }
                    },
                    ConditionExpression = "attribute_not_exists(LockId) OR (#s <> :done AND ExpiresAt < :now)",
                    ExpressionAttributeNames = new Dictionary<string, string> { { "#s", "Status" } },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":done", new AttributeValue { S = StatusCompleted } },
                        { ":now", new AttributeValue { N = now.ToUnixTimeSeconds().ToString() } }
                    }
                }).ConfigureAwait(false);
                return true;
            }
            catch (ConditionalCheckFailedException)
            {
                return false;
            }
        }

        private Task MarkCompletedAsync(IAmazonDynamoDB ddb, string lockId)
        {
            return ddb.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _lockTable,
                Key = new Dictionary<string, AttributeValue> { { "LockId", new AttributeValue { S = lockId } } },
                UpdateExpression = "SET #s = :done REMOVE ExpiresAt",
                ConditionExpression = "Owner = :me",
                ExpressionAttributeNames = new Dictionary<string, string> { { "#s", "Status" } },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":done", new AttributeValue { S = StatusCompleted } },
                    { ":me", new AttributeValue { S = _instanceId } }
                }
            });
        }

        private async Task ReleaseAsync(IAmazonDynamoDB ddb, string lockId)
        {
            try
            {
                await ddb.DeleteItemAsync(new DeleteItemRequest
                {
                    TableName = _lockTable,
                    Key = new Dictionary<string, AttributeValue> { { "LockId", new AttributeValue { S = lockId } } },
                    ConditionExpression = "Owner = :me",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":me", new AttributeValue { S = _instanceId } }
                    }
                }).ConfigureAwait(false);
            }
            catch (ConditionalCheckFailedException)
            {
                // Lock no longer owned by this instance; nothing to release.
            }
        }

        private static int ReadInt(string name, int defaultValue)
        {
            int value;
            return int.TryParse(Environment.GetEnvironmentVariable(name), out value) && value > 0 ? value : defaultValue;
        }
    }
}
