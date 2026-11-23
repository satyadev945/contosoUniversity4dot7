using System;
using System.Collections.Generic;
using System.Threading;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace ContosoUniversity.Infrastructure
{
    /// <summary>
    /// Coordinates one-time, cluster-wide initialization across all scaled instances using a DynamoDB
    /// conditional write as a distributed lock. The table name is supplied via INIT_LOCK_TABLE (partition key
    /// "LockId" of type String). Exactly one instance executes the initialization; the others wait until it
    /// completes (or the lock expires) and then continue. If INIT_LOCK_TABLE is not set (local development)
    /// the initialization simply runs in-process.
    /// </summary>
    public static class DistributedInitializer
    {
        public const string TableVariable = "INIT_LOCK_TABLE";
        private static readonly string InstanceId = Environment.MachineName + "-" + Guid.NewGuid().ToString("N");

        public static void RunOnce(string initializationName, Action initialization, TimeSpan? leaseDuration = null, TimeSpan? waitTimeout = null)
        {
            var table = Environment.GetEnvironmentVariable(TableVariable);
            if (string.IsNullOrWhiteSpace(table))
            {
                initialization();
                return;
            }

            var lease = leaseDuration ?? TimeSpan.FromMinutes(5);
            var timeout = waitTimeout ?? TimeSpan.FromMinutes(5);
            var deadline = DateTime.UtcNow + timeout;
            var lockId = "init:" + initializationName;

            using (var client = new AmazonDynamoDBClient())
            {
                while (true)
                {
                    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    try
                    {
                        // Acquire when no item exists, or a previous holder's lease expired without completing.
                        client.PutItemAsync(new PutItemRequest
                        {
                            TableName = table,
                            Item = new Dictionary<string, AttributeValue>
                            {
                                { "LockId", new AttributeValue { S = lockId } },
                                { "Owner", new AttributeValue { S = InstanceId } },
                                { "Status", new AttributeValue { S = "IN_PROGRESS" } },
                                { "ExpiresAt", new AttributeValue { N = (now + (long)lease.TotalSeconds).ToString() } }
                            },
                            ConditionExpression = "attribute_not_exists(LockId) OR (#s <> :done AND ExpiresAt < :now)",
                            ExpressionAttributeNames = new Dictionary<string, string> { { "#s", "Status" } },
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                            {
                                { ":done", new AttributeValue { S = "COMPLETED" } },
                                { ":now", new AttributeValue { N = now.ToString() } }
                            }
                        }).GetAwaiter().GetResult();
                    }
                    catch (ConditionalCheckFailedException)
                    {
                        // Another instance holds the lock or has already completed the initialization.
                        var existing = client.GetItemAsync(new GetItemRequest
                        {
                            TableName = table,
                            Key = new Dictionary<string, AttributeValue> { { "LockId", new AttributeValue { S = lockId } } },
                            ConsistentRead = true
                        }).GetAwaiter().GetResult();

                        if (existing.Item != null && existing.Item.ContainsKey("Status") && existing.Item["Status"].S == "COMPLETED")
                        {
                            return;
                        }
                        if (DateTime.UtcNow > deadline)
                        {
                            throw new TimeoutException("Timed out waiting for distributed initialization '" + initializationName + "'.");
                        }
                        Thread.Sleep(TimeSpan.FromSeconds(2));
                        continue;
                    }

                    // Lock acquired: this instance performs the initialization.
                    initialization();

                    client.UpdateItemAsync(new UpdateItemRequest
                    {
                        TableName = table,
                        Key = new Dictionary<string, AttributeValue> { { "LockId", new AttributeValue { S = lockId } } },
                        UpdateExpression = "SET #s = :done",
                        ExpressionAttributeNames = new Dictionary<string, string> { { "#s", "Status" } },
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue> { { ":done", new AttributeValue { S = "COMPLETED" } } }
                    }).GetAwaiter().GetResult();
                    return;
                }
            }
        }
    }
}
