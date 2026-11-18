using System;
using System.Configuration;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace ContosoUniversity.Caching
{
    /// <summary>
    /// AWS ElastiCache (Redis) backed cache. The endpoint is read from the ELASTICACHE_ENDPOINT
    /// environment variable (host:port), falling back to the "ElastiCache:Endpoint" app setting.
    /// When no endpoint is configured, or ElastiCache is unreachable, the cache is bypassed and
    /// callers transparently fall back to the database.
    /// </summary>
    public class RedisCacheService : ICacheService
    {
        private static readonly Lazy<Task<ConnectionMultiplexer>> LazyConnection =
            new Lazy<Task<ConnectionMultiplexer>>(ConnectAsync);

        private static readonly Lazy<RedisCacheService> LazyInstance =
            new Lazy<RedisCacheService>(() => new RedisCacheService());

        public static RedisCacheService Instance
        {
            get { return LazyInstance.Value; }
        }

        private static string GetEndpoint()
        {
            var endpoint = Environment.GetEnvironmentVariable("ELASTICACHE_ENDPOINT");
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                endpoint = ConfigurationManager.AppSettings["ElastiCache:Endpoint"];
            }
            return endpoint;
        }

        private static async Task<ConnectionMultiplexer> ConnectAsync()
        {
            var endpoint = GetEndpoint();
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return null;
            }

            try
            {
                var options = ConfigurationOptions.Parse(endpoint);
                options.AbortOnConnectFail = false;
                options.ConnectTimeout = 2000;
                options.Ssl = string.Equals(
                    Environment.GetEnvironmentVariable("ELASTICACHE_TLS"), "true", StringComparison.OrdinalIgnoreCase);
                var password = Environment.GetEnvironmentVariable("ELASTICACHE_AUTH_TOKEN");
                if (!string.IsNullOrEmpty(password))
                {
                    options.Password = password;
                }
                return await ConnectionMultiplexer.ConnectAsync(options).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static async Task<IDatabase> GetDatabaseAsync()
        {
            var connection = await LazyConnection.Value.ConfigureAwait(false);
            return connection != null && connection.IsConnected ? connection.GetDatabase() : null;
        }

        public async Task<T> GetAsync<T>(string key) where T : class
        {
            try
            {
                var db = await GetDatabaseAsync().ConfigureAwait(false);
                if (db == null) return null;
                var value = await db.StringGetAsync(key).ConfigureAwait(false);
                return value.IsNullOrEmpty ? null : JsonConvert.DeserializeObject<T>(value);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan ttl) where T : class
        {
            try
            {
                var db = await GetDatabaseAsync().ConfigureAwait(false);
                if (db == null) return;
                await db.StringSetAsync(key, JsonConvert.SerializeObject(value), ttl).ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                var db = await GetDatabaseAsync().ConfigureAwait(false);
                if (db == null) return;
                await db.KeyDeleteAsync(key).ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
        }
    }
}
