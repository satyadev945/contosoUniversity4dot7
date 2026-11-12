using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace ContosoUniversity.Infrastructure
{
    /// <summary>
    /// Runs one-time, cluster-wide initialization exactly once across all scaled instances using a
    /// DynamoDB conditional-write lock. Table name comes from INIT_LOCK_TABLE (partition key "LockId", string).
    /// If INIT_LOCK_TABLE is not set (local development) the action runs directly.
    /// </summary>
    public static class DistributedInitializer
    {
        private const string LockKeyAttribute = "LockId";
        private static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan WaitTimeout = TimeSpan.FromMinutes(2);

        public static void RunOnce(string lockName, Action initialization)
        {
            var table = Environment.GetEnvironmentVariable("INIT_LOCK_TABLE");
            if (string.IsNullOrWhiteSpace(table))
            {
                initialization();
                return;
            }

            Task.Run(() => RunOnceAsync(table, lockName, initialization)).GetAwaiter().GetResult();
        }

        private static async Task RunOnceAsync(string table, string lockName, Action initialization)
        {
            var owner = Environment.MachineName + ":" + Guid.NewGuid().ToString("N");
            var deadline = DateTime.UtcNow + WaitTimeout;

            using (var client = new AmazonDynamoDBClient())
            {
                while (true)
                {
                    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    try
                    {
                        // Acquire lock if absent, expired, or (completed state is handled below)
                        await client.PutItemAsync(new PutItemRequest
                        {
                            TableName = table,
                            Item = new Dictionary<string, AttributeValue>
                            {
                                { LockKeyAttribute, new AttributeValue { S = lockName } },
                                { "Owner", new AttributeValue { S = owner } },
                                { "Status", new AttributeValue { S = "RUNNING" } },
                                { "ExpiresAt", new AttributeValue { N = (now + (long)LockTtl.TotalSeconds).ToString() } }
                            },
                            ConditionExpression = "attribute_not_exists(#k) OR (#s <> :done AND ExpiresAt < :now)",
                            ExpressionAttributeNames = new Dictionary<string, string> { { "#k", LockKeyAttribute }, { "#s", "Status" } },
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                            {
                                { ":done", new AttributeValue { S = "DONE" } },
                                { ":now", new AttributeValue { N = now.ToString() } }
                            }
                        });
                    }
                    catch (ConditionalCheckFailedException)
                    {
                        // Another instance holds the lock or has already completed initialization.
                        var existing = await client.GetItemAsync(table, new Dictionary<string, AttributeValue>
                        {
                            { LockKeyAttribute, new AttributeValue { S = lockName } }
                        });
                        if (existing.Item != null && existing.Item.TryGetValue("Status", out var status) && status.S == "DONE")
                        {
                            return;
                        }
                        if (DateTime.UtcNow > deadline)
                        {
                            throw new TimeoutException("Timed out waiting for distributed initialization '" + lockName + "'.");
                        }
                        await Task.Delay(TimeSpan.FromSeconds(2));
                        continue;
                    }

                    try
                    {
                        initialization();
                        await client.UpdateItemAsync(new UpdateItemRequest
                        {
                            TableName = table,
                            Key = new Dictionary<string, AttributeValue> { { LockKeyAttribute, new AttributeValue { S = lockName } } },
                            UpdateExpression = "SET #s = :done",
                            ExpressionAttributeNames = new Dictionary<string, string> { { "#s", "Status" } },
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue> { { ":done", new AttributeValue { S = "DONE" } } }
                        });
                    }
                    catch
                    {
                        // Release the lock so another instance can retry.
                        await client.DeleteItemAsync(table, new Dictionary<string, AttributeValue>
                        {
                            { LockKeyAttribute, new AttributeValue { S = lockName } }
                        });
                        throw;
                    }
                    return;
                }
            }
        }
    }
}
