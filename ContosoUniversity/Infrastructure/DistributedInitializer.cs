using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace ContosoUniversity.Infrastructure
{
    /// <summary>
    /// Runs one-time, cluster-wide initialization (e.g. database initialization/seeding) exactly once
    /// across all scaled instances using a DynamoDB conditional-write lock. Instances that lose the race
    /// wait for the winner to finish and then skip the work, so all instances end up with a consistent setup.
    ///
    /// Configuration (environment variables or Parameter Store keys):
    ///   INIT_LOCK_TABLE        DynamoDB table name (partition key: LockId, String). If unset, the work runs locally.
    ///   INIT_LOCK_TTL_SECONDS  Lease time of an in-progress lock (default 300).
    ///   INIT_LOCK_WAIT_SECONDS Maximum time to wait for another instance (default 120).
    /// </summary>
    public static class DistributedInitializer
    {
        public const string TableEnvVar = "INIT_LOCK_TABLE";
        private const string KeyAttr = "LockId";
        private const string StatusAttr = "Status";
        private const string OwnerAttr = "Owner";
        private const string ExpiresAttr = "ExpiresAt";
        private const string InProgress = "InProgress";
        private const string Completed = "Completed";

        public static void RunOnce(string name, Action work)
        {
            var table = ParameterStoreSettings.Get(TableEnvVar);
            if (string.IsNullOrWhiteSpace(table))
            {
                // No distributed store configured (e.g. local development): run locally.
                work();
                return;
            }

            // Scope the lock to the deployed build so a new release re-runs initialization once.
            var lockId = name + ":" + Assembly.GetExecutingAssembly().GetName().Version;
            var owner = Environment.MachineName + ":" + Guid.NewGuid().ToString("N");
            var ttl = GetInt("INIT_LOCK_TTL_SECONDS", 300);
            var maxWait = TimeSpan.FromSeconds(GetInt("INIT_LOCK_WAIT_SECONDS", 120));
            var started = DateTime.UtcNow;

            using (var client = CreateClient())
            {
                while (true)
                {
                    if (TryAcquire(client, table, lockId, owner, ttl))
                    {
                        try
                        {
                            work();
                            MarkCompleted(client, table, lockId, owner);
                        }
                        catch
                        {
                            Release(client, table, lockId, owner);
                            throw;
                        }
                        return;
                    }

                    if (IsCompleted(client, table, lockId))
                    {
                        return;
                    }

                    if (DateTime.UtcNow - started > maxWait)
                    {
                        throw new TimeoutException("Timed out waiting for distributed initialization '" + lockId + "'.");
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                }
            }
        }

        private static bool TryAcquire(IAmazonDynamoDB client, string table, string lockId, string owner, int ttl)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            try
            {
                client.PutItem(new PutItemRequest
                {
                    TableName = table,
                    Item = new Dictionary<string, AttributeValue>
                    {
                        { KeyAttr, new AttributeValue { S = lockId } },
                        { StatusAttr, new AttributeValue { S = InProgress } },
                        { OwnerAttr, new AttributeValue { S = owner } },
                        { ExpiresAttr, new AttributeValue { N = (now + ttl).ToString(CultureInfo.InvariantCulture) } }
                    },
                    ConditionExpression = "attribute_not_exists(#k) OR (#s = :inprogress AND #e < :now)",
                    ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        { "#k", KeyAttr }, { "#s", StatusAttr }, { "#e", ExpiresAttr }
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":inprogress", new AttributeValue { S = InProgress } },
                        { ":now", new AttributeValue { N = now.ToString(CultureInfo.InvariantCulture) } }
                    }
                });
                return true;
            }
            catch (ConditionalCheckFailedException)
            {
                return false;
            }
        }

        private static bool IsCompleted(IAmazonDynamoDB client, string table, string lockId)
        {
            var response = client.GetItem(new GetItemRequest
            {
                TableName = table,
                ConsistentRead = true,
                Key = new Dictionary<string, AttributeValue> { { KeyAttr, new AttributeValue { S = lockId } } }
            });
            AttributeValue status;
            return response.Item != null
                   && response.Item.TryGetValue(StatusAttr, out status)
                   && status.S == Completed;
        }

        private static void MarkCompleted(IAmazonDynamoDB client, string table, string lockId, string owner)
        {
            client.UpdateItem(new UpdateItemRequest
            {
                TableName = table,
                Key = new Dictionary<string, AttributeValue> { { KeyAttr, new AttributeValue { S = lockId } } },
                UpdateExpression = "SET #s = :done REMOVE #e",
                ConditionExpression = "#o = :owner",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#s", StatusAttr }, { "#e", ExpiresAttr }, { "#o", OwnerAttr }
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":done", new AttributeValue { S = Completed } },
                    { ":owner", new AttributeValue { S = owner } }
                }
            });
        }

        private static void Release(IAmazonDynamoDB client, string table, string lockId, string owner)
        {
            try
            {
                client.DeleteItem(new DeleteItemRequest
                {
                    TableName = table,
                    Key = new Dictionary<string, AttributeValue> { { KeyAttr, new AttributeValue { S = lockId } } },
                    ConditionExpression = "#o = :owner",
                    ExpressionAttributeNames = new Dictionary<string, string> { { "#o", OwnerAttr } },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":owner", new AttributeValue { S = owner } }
                    }
                });
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Unable to release initialization lock '" + lockId + "': " + ex.Message);
            }
        }

        private static int GetInt(string key, int defaultValue)
        {
            int v;
            return int.TryParse(ParameterStoreSettings.Get(key), out v) && v > 0 ? v : defaultValue;
        }

        private static AmazonDynamoDBClient CreateClient()
        {
            var region = Environment.GetEnvironmentVariable("AWS_REGION")
                         ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION");
            return string.IsNullOrWhiteSpace(region)
                ? new AmazonDynamoDBClient()
                : new AmazonDynamoDBClient(RegionEndpoint.GetBySystemName(region));
        }
    }
}
