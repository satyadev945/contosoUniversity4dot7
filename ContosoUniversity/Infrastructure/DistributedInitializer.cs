using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace ContosoUniversity.Infrastructure
{
    /// <summary>
    /// Replaces the instance-local initialization that used to live in Global.asax Application_Start.
    /// - Centralized configuration is read from AWS Systems Manager Parameter Store.
    /// - One-time, cluster-wide initialization is coordinated with a DynamoDB conditional-write lock so that
    ///   it runs exactly once across all scaled instances; the other instances wait/skip.
    /// Per-process setup (e.g. EF interceptors) still runs on every instance but is driven by the shared configuration.
    ///
    /// Environment variables:
    ///   INIT_LOCK_TABLE        DynamoDB table (partition key "LockId", string). Unset => distributed locking disabled (local dev).
    ///   SSM_PARAMETER_PREFIX   Parameter Store prefix (default "/contosouniversity").
    ///   INIT_LOCK_TTL_SECONDS  Lock lease length (default 120).
    ///   AWS_REGION             Standard AWS region setting.
    /// </summary>
    public class DistributedInitializer
    {
        private const string LockId = "ContosoUniversity-Initialization";

        private readonly string _lockTable = Environment.GetEnvironmentVariable("INIT_LOCK_TABLE");
        private readonly string _prefix = (Environment.GetEnvironmentVariable("SSM_PARAMETER_PREFIX") ?? "/contosouniversity").TrimEnd('/');
        private readonly int _ttlSeconds = ParseInt(Environment.GetEnvironmentVariable("INIT_LOCK_TTL_SECONDS"), 120);
        private readonly string _ownerId = Environment.MachineName + ":" + Guid.NewGuid().ToString("N");

        /// <summary>Shared configuration loaded from Parameter Store (empty when unavailable).</summary>
        public IDictionary<string, string> Configuration { get; private set; } = new Dictionary<string, string>();

        /// <summary>True when this instance executed the cluster-wide one-time initialization.</summary>
        public bool IsLeader { get; private set; }

        public void Initialize()
        {
            try
            {
                Task.Run(InitializeAsync).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                // Never prevent an instance from starting because the coordination services are unreachable.
                Console.Error.WriteLine("{\"level\":\"warn\",\"message\":\"Distributed initialization skipped\",\"error\":\"" +
                    ex.Message.Replace("\"", "'") + "\"}");
            }
        }

        private async Task InitializeAsync()
        {
            await LoadConfigurationAsync();

            if (string.IsNullOrEmpty(_lockTable))
            {
                return;
            }

            using (var dynamo = new AmazonDynamoDBClient())
            {
                if (await TryAcquireLockAsync(dynamo))
                {
                    IsLeader = true;
                    try
                    {
                        await RunClusterWideInitializationAsync();
                    }
                    finally
                    {
                        await ReleaseLockAsync(dynamo);
                    }
                }
                else
                {
                    // Another instance is initializing; wait (bounded) until its lease is released.
                    var deadline = DateTime.UtcNow.AddSeconds(_ttlSeconds);
                    while (DateTime.UtcNow < deadline && await IsLockHeldAsync(dynamo))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2));
                    }
                    await LoadConfigurationAsync();
                }
            }
        }

        private async Task LoadConfigurationAsync()
        {
            using (var ssm = new AmazonSimpleSystemsManagementClient())
            {
                var request = new GetParametersByPathRequest
                {
                    Path = _prefix + "/config",
                    Recursive = true,
                    WithDecryption = true
                };
                var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                GetParametersByPathResponse response;
                do
                {
                    response = await ssm.GetParametersByPathAsync(request);
                    foreach (var p in response.Parameters)
                    {
                        config[p.Name.Substring(p.Name.LastIndexOf('/') + 1)] = p.Value;
                    }
                    request.NextToken = response.NextToken;
                } while (!string.IsNullOrEmpty(response.NextToken));
                Configuration = config;
            }
        }

        private async Task RunClusterWideInitializationAsync()
        {
            // One-time shared initialization: record which instance initialized the cluster and when.
            using (var ssm = new AmazonSimpleSystemsManagementClient())
            {
                await ssm.PutParameterAsync(new PutParameterRequest
                {
                    Name = _prefix + "/state/last-initialized",
                    Value = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) + " by " + _ownerId,
                    Type = ParameterType.String,
                    Overwrite = true
                });
            }
        }

        private async Task<bool> TryAcquireLockAsync(IAmazonDynamoDB dynamo)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            try
            {
                await dynamo.PutItemAsync(new PutItemRequest
                {
                    TableName = _lockTable,
                    Item = new Dictionary<string, AttributeValue>
                    {
                        ["LockId"] = new AttributeValue { S = LockId },
                        ["Owner"] = new AttributeValue { S = _ownerId },
                        ["ExpiresAt"] = new AttributeValue { N = (now + _ttlSeconds).ToString(CultureInfo.InvariantCulture) }
                    },
                    ConditionExpression = "attribute_not_exists(LockId) OR ExpiresAt < :now",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        [":now"] = new AttributeValue { N = now.ToString(CultureInfo.InvariantCulture) }
                    }
                });
                return true;
            }
            catch (ConditionalCheckFailedException)
            {
                return false;
            }
        }

        private async Task<bool> IsLockHeldAsync(IAmazonDynamoDB dynamo)
        {
            var result = await dynamo.GetItemAsync(_lockTable, new Dictionary<string, AttributeValue>
            {
                ["LockId"] = new AttributeValue { S = LockId }
            }, true);
            if (result.Item == null || result.Item.Count == 0) return false;
            long expires;
            return long.TryParse(result.Item["ExpiresAt"].N, NumberStyles.Integer, CultureInfo.InvariantCulture, out expires)
                && expires > DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        private async Task ReleaseLockAsync(IAmazonDynamoDB dynamo)
        {
            try
            {
                await dynamo.DeleteItemAsync(new DeleteItemRequest
                {
                    TableName = _lockTable,
                    Key = new Dictionary<string, AttributeValue> { ["LockId"] = new AttributeValue { S = LockId } },
                    ConditionExpression = "Owner = :o",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        [":o"] = new AttributeValue { S = _ownerId }
                    }
                });
            }
            catch (ConditionalCheckFailedException)
            {
                // Lease expired and was taken over by another instance; nothing to release.
            }
        }

        private static int ParseInt(string value, int fallback)
        {
            int result;
            return int.TryParse(value, out result) && result > 0 ? result : fallback;
        }
    }
}
