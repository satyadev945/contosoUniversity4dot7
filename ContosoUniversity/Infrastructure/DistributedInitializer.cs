using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using ContosoUniversity.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ContosoUniversity.Infrastructure
{
    /// <summary>
    /// Replaces the instance-specific Global.asax Application_Start initialization.
    /// Shared settings are read from AWS Systems Manager Parameter Store and one-time
    /// (cluster-wide) initialization is guarded by a DynamoDB conditional-write lock so
    /// that exactly one scaled instance performs it. Process-local registrations
    /// (e.g. DbInterception) must still run on every instance and stay in Startup.
    ///
    /// Environment variables:
    ///   INIT_PARAMETER_PREFIX  Parameter Store path prefix (default: /contosouniversity)
    ///   INIT_LOCK_TABLE        DynamoDB table (partition key "LockId", string, optional TTL attribute "ExpiresAt")
    ///   INIT_LOCK_TTL_SECONDS  lock lease time (default: 120)
    /// </summary>
    public class DistributedInitializer
    {
        private const string LockId = "contosouniversity-app-initialization";

        private readonly ILogger _logger;
        private readonly IAmazonSimpleSystemsManagement _ssm;
        private readonly IAmazonDynamoDB _dynamo;
        private readonly string _prefix;
        private readonly string _lockTable;
        private readonly int _lockTtlSeconds;
        private readonly string _instanceId = Environment.MachineName + ":" + Guid.NewGuid().ToString("N");

        public DistributedInitializer(ILogger logger)
            : this(logger, new AmazonSimpleSystemsManagementClient(), new AmazonDynamoDBClient())
        {
        }

        public DistributedInitializer(ILogger logger, IAmazonSimpleSystemsManagement ssm, IAmazonDynamoDB dynamo)
        {
            _logger = logger;
            _ssm = ssm;
            _dynamo = dynamo;
            _prefix = (Environment.GetEnvironmentVariable("INIT_PARAMETER_PREFIX") ?? "/contosouniversity").TrimEnd('/');
            _lockTable = Environment.GetEnvironmentVariable("INIT_LOCK_TABLE");
            int ttl;
            _lockTtlSeconds = int.TryParse(Environment.GetEnvironmentVariable("INIT_LOCK_TTL_SECONDS"), out ttl) && ttl > 0 ? ttl : 120;
        }

        /// <summary>Loads all shared parameters under the prefix from Parameter Store.</summary>
        public async Task<IDictionary<string, string>> LoadSharedConfigurationAsync()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string nextToken = null;
            do
            {
                var response = await _ssm.GetParametersByPathAsync(new GetParametersByPathRequest
                {
                    Path = _prefix,
                    Recursive = true,
                    WithDecryption = true,
                    NextToken = nextToken
                });
                foreach (var p in response.Parameters)
                {
                    result[p.Name.Substring(_prefix.Length).TrimStart('/')] = p.Value;
                }
                nextToken = response.NextToken;
            } while (!string.IsNullOrEmpty(nextToken));
            return result;
        }

        /// <summary>
        /// Runs <paramref name="oneTimeInitialization"/> once across all instances for the given
        /// initialization version. Instances that lose the lock wait for the winner to publish the
        /// completion marker in Parameter Store.
        /// </summary>
        public async Task EnsureInitializedAsync(string initVersion, Func<Task> oneTimeInitialization)
        {
            if (string.IsNullOrEmpty(_lockTable))
            {
                _logger.Warning("INIT_LOCK_TABLE is not configured; skipping distributed initialization.");
                return;
            }

            var markerName = _prefix + "/initialized-version";
            if (await ReadMarkerAsync(markerName) == initVersion)
            {
                _logger.Information("Initialization version {0} already completed.", initVersion);
                return;
            }

            var deadline = DateTime.UtcNow.AddSeconds(_lockTtlSeconds * 2);
            while (DateTime.UtcNow < deadline)
            {
                if (await TryAcquireLockAsync())
                {
                    try
                    {
                        if (await ReadMarkerAsync(markerName) != initVersion)
                        {
                            await oneTimeInitialization();
                            await _ssm.PutParameterAsync(new PutParameterRequest
                            {
                                Name = markerName,
                                Value = initVersion,
                                Type = ParameterType.String,
                                Overwrite = true
                            });
                            _logger.Information("Initialization version {0} completed by {1}.", initVersion, _instanceId);
                        }
                    }
                    finally
                    {
                        await ReleaseLockAsync();
                    }
                    return;
                }

                // Another instance is initializing; wait for it to finish.
                await Task.Delay(TimeSpan.FromSeconds(2));
                if (await ReadMarkerAsync(markerName) == initVersion)
                {
                    return;
                }
            }

            _logger.Warning("Timed out waiting for distributed initialization version {0}.", initVersion);
        }

        private async Task<string> ReadMarkerAsync(string name)
        {
            try
            {
                var response = await _ssm.GetParameterAsync(new GetParameterRequest { Name = name });
                return response.Parameter.Value;
            }
            catch (ParameterNotFoundException)
            {
                return null;
            }
        }

        private async Task<bool> TryAcquireLockAsync()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            try
            {
                await _dynamo.PutItemAsync(new PutItemRequest
                {
                    TableName = _lockTable,
                    Item = new Dictionary<string, AttributeValue>
                    {
                        { "LockId", new AttributeValue { S = LockId } },
                        { "Owner", new AttributeValue { S = _instanceId } },
                        { "ExpiresAt", new AttributeValue { N = (now + _lockTtlSeconds).ToString() } }
                    },
                    // Acquire if free or the previous lease expired.
                    ConditionExpression = "attribute_not_exists(LockId) OR ExpiresAt < :now",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
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

        private async Task ReleaseLockAsync()
        {
            try
            {
                await _dynamo.DeleteItemAsync(new DeleteItemRequest
                {
                    TableName = _lockTable,
                    Key = new Dictionary<string, AttributeValue> { { "LockId", new AttributeValue { S = LockId } } },
                    ConditionExpression = "Owner = :owner",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":owner", new AttributeValue { S = _instanceId } }
                    }
                });
            }
            catch (ConditionalCheckFailedException)
            {
                // Lease expired and was taken over by another instance.
            }
        }
    }
}
