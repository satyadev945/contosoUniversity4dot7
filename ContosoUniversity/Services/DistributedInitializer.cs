using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// Runs one-time, cluster-wide initialization exactly once across all scaled instances, using a
    /// DynamoDB conditional write as a distributed lock + completion marker.
    /// Replaces the per-instance Global.asax Application_Start initialization.
    /// Table name: DISTRIBUTED_LOCK_TABLE env var / "DistributedInit:TableName" (hash key: LockId, String).
    /// </summary>
    public interface IDistributedInitializer
    {
        /// <summary>Returns true if this instance executed the action; false if another instance did/does it.</summary>
        Task<bool> RunOnceAsync(string name, Func<Task> action, CancellationToken cancellationToken);
    }

    public class DynamoDbDistributedInitializer : IDistributedInitializer
    {
        private const string KeyAttr = "LockId";
        private const string OwnerAttr = "Owner";
        private const string ExpiresAttr = "ExpiresAt";
        private const string CompletedAttr = "Completed";

        private readonly IAmazonDynamoDB _dynamo;
        private readonly string _tableName;
        private readonly TimeSpan _leaseTime;
        private readonly string _owner = Environment.MachineName + ":" + Guid.NewGuid().ToString("N");
        private readonly ILogger<DynamoDbDistributedInitializer> _logger;

        public DynamoDbDistributedInitializer(IAmazonDynamoDB dynamo, IConfiguration configuration,
            ILogger<DynamoDbDistributedInitializer> logger)
        {
            _dynamo = dynamo;
            _logger = logger;
            _tableName = configuration["DISTRIBUTED_LOCK_TABLE"] ?? configuration["DistributedInit:TableName"] ?? "contoso-init-locks";
            int leaseSeconds;
            _leaseTime = TimeSpan.FromSeconds(int.TryParse(configuration["DistributedInit:LeaseSeconds"], out leaseSeconds) ? leaseSeconds : 120);
        }

        public async Task<bool> RunOnceAsync(string name, Func<Task> action, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var expires = DateTimeOffset.UtcNow.Add(_leaseTime).ToUnixTimeSeconds();

            try
            {
                // Acquire when: no item, or lease expired and not completed.
                await _dynamo.PutItemAsync(new PutItemRequest
                {
                    TableName = _tableName,
                    Item = new Dictionary<string, AttributeValue>
                    {
                        { KeyAttr, new AttributeValue { S = name } },
                        { OwnerAttr, new AttributeValue { S = _owner } },
                        { ExpiresAttr, new AttributeValue { N = expires.ToString() } },
                        { CompletedAttr, new AttributeValue { BOOL = false } }
                    },
                    ConditionExpression = "attribute_not_exists(#k) OR (#c = :false AND #e < :now)",
                    ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        { "#k", KeyAttr }, { "#c", CompletedAttr }, { "#e", ExpiresAttr }
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":false", new AttributeValue { BOOL = false } },
                        { ":now", new AttributeValue { N = now.ToString() } }
                    }
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (ConditionalCheckFailedException)
            {
                _logger.LogInformation("Initialization '{Name}' already completed or in progress on another instance; skipping.", name);
                return false;
            }

            try
            {
                await action().ConfigureAwait(false);
            }
            catch
            {
                // Release the lock so another instance can retry.
                await _dynamo.DeleteItemAsync(_tableName,
                    new Dictionary<string, AttributeValue> { { KeyAttr, new AttributeValue { S = name } } },
                    cancellationToken).ConfigureAwait(false);
                throw;
            }

            // Mark as permanently completed (owner-checked).
            await _dynamo.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue> { { KeyAttr, new AttributeValue { S = name } } },
                UpdateExpression = "SET #c = :true",
                ConditionExpression = "#o = :owner",
                ExpressionAttributeNames = new Dictionary<string, string> { { "#c", CompletedAttr }, { "#o", OwnerAttr } },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":true", new AttributeValue { BOOL = true } },
                    { ":owner", new AttributeValue { S = _owner } }
                }
            }, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Initialization '{Name}' completed by this instance.", name);
            return true;
        }
    }

    /// <summary>
    /// Hosted service performing cluster-wide one-time startup work (database initialization) under the
    /// DynamoDB distributed lock. Per-process wiring (EF interceptors) stays in Startup since it must run on every instance.
    /// </summary>
    public class DistributedStartupInitializer : IHostedService
    {
        private readonly IDistributedInitializer _initializer;
        private readonly ILogger<DistributedStartupInitializer> _logger;

        public DistributedStartupInitializer(IDistributedInitializer initializer, ILogger<DistributedStartupInitializer> logger)
        {
            _initializer = initializer;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _initializer.RunOnceAsync("contoso-database-initialization", () => Task.Run(() =>
                {
                    using (var context = new ContosoUniversity.DAL.SchoolContext())
                    {
                        context.Database.Initialize(false);
                    }
                }), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Never block instance startup on the coordination store; lazy EF initialization still applies.
                _logger.LogWarning(ex, "Distributed initialization could not be coordinated; continuing startup.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
