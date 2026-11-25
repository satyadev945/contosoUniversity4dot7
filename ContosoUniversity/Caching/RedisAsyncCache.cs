using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace ContosoUniversity.Caching
{
    /// <summary>
    /// AWS ElastiCache (Redis) backed async cache. The endpoint is read from the
    /// REDIS_ENDPOINT environment variable (host:port[,options]). When the variable is
    /// not set, or Redis is unreachable, the cache transparently falls back to the
    /// data source so the application keeps working.
    /// </summary>
    public class RedisAsyncCache : IAsyncCache
    {
        private const string KeyPrefix = "contosouniversity:";

        private static readonly Lazy<Task<ConnectionMultiplexer>> Connection =
            new Lazy<Task<ConnectionMultiplexer>>(ConnectAsync);

        public static readonly IAsyncCache Instance = new RedisAsyncCache();

        private static async Task<ConnectionMultiplexer> ConnectAsync()
        {
            var endpoint = Environment.GetEnvironmentVariable("REDIS_ENDPOINT");
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return null;
            }

            try
            {
                var options = ConfigurationOptions.Parse(endpoint);
                options.AbortOnConnectFail = false;
                options.ConnectTimeout = 2000;
                var useSsl = Environment.GetEnvironmentVariable("REDIS_USE_SSL");
                if (string.Equals(useSsl, "true", StringComparison.OrdinalIgnoreCase))
                {
                    options.Ssl = true;
                }
                var password = Environment.GetEnvironmentVariable("REDIS_AUTH_TOKEN");
                if (!string.IsNullOrEmpty(password))
                {
                    options.Password = password;
                }
                return await ConnectionMultiplexer.ConnectAsync(options).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Redis connection failed, caching disabled: " + ex.Message);
                return null;
            }
        }

        public async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl) where T : class
        {
            IDatabase redis = null;
            var fullKey = KeyPrefix + key;

            try
            {
                var connection = await Connection.Value.ConfigureAwait(false);
                if (connection != null && connection.IsConnected)
                {
                    redis = connection.GetDatabase();
                    var cached = await redis.StringGetAsync(fullKey).ConfigureAwait(false);
                    if (cached.HasValue)
                    {
                        return JsonConvert.DeserializeObject<T>((string)cached);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Redis read failed: " + ex.Message);
                redis = null;
            }

            var value = await factory().ConfigureAwait(false);

            if (redis != null && value != null)
            {
                try
                {
                    await redis.StringSetAsync(fullKey, JsonConvert.SerializeObject(value), ttl).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Redis write failed: " + ex.Message);
                }
            }

            return value;
        }
    }
}
