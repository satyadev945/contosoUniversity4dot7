using System;
using System.Configuration;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// AWS ElastiCache (Redis) backed cache. The endpoint is read from the
    /// ELASTICACHE_ENDPOINT environment variable (host:port[,options]) so no
    /// connection details are hard-coded. If not configured, or if the cache is
    /// unavailable, data is loaded directly from the data source (cache-aside).
    /// All operations are asynchronous so request threads are never blocked.
    /// </summary>
    public class ElastiCacheService : ICacheService
    {
        private static readonly Lazy<Task<ConnectionMultiplexer>> LazyConnection =
            new Lazy<Task<ConnectionMultiplexer>>(ConnectAsync);

        private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

        private static async Task<ConnectionMultiplexer> ConnectAsync()
        {
            var endpoint = Environment.GetEnvironmentVariable("ELASTICACHE_ENDPOINT");
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return null;
            }
            var options = ConfigurationOptions.Parse(endpoint);
            options.AbortOnConnectFail = false;
            var useSsl = Environment.GetEnvironmentVariable("ELASTICACHE_USE_SSL");
            if (string.Equals(useSsl, "true", StringComparison.OrdinalIgnoreCase))
            {
                options.Ssl = true;
            }
            var password = Environment.GetEnvironmentVariable("ELASTICACHE_AUTH_TOKEN");
            if (!string.IsNullOrEmpty(password))
            {
                options.Password = password;
            }
            return await ConnectionMultiplexer.ConnectAsync(options).ConfigureAwait(false);
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null) where T : class
        {
            IDatabase cache = null;
            try
            {
                var connection = await LazyConnection.Value.ConfigureAwait(false);
                if (connection != null && connection.IsConnected)
                {
                    cache = connection.GetDatabase();
                    var cached = await cache.StringGetAsync(key).ConfigureAwait(false);
                    if (cached.HasValue)
                    {
                        return JsonConvert.DeserializeObject<T>(cached);
                    }
                }
            }
            catch (Exception)
            {
                cache = null; // cache unavailable: fall back to the data source
            }

            var value = await factory().ConfigureAwait(false);

            if (cache != null && value != null)
            {
                try
                {
                    await cache.StringSetAsync(key, JsonConvert.SerializeObject(value), ttl ?? DefaultTtl).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // best-effort cache write
                }
            }
            return value;
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                var connection = await LazyConnection.Value.ConfigureAwait(false);
                if (connection != null && connection.IsConnected)
                {
                    await connection.GetDatabase().KeyDeleteAsync(key).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                // best-effort cache invalidation
            }
        }
    }
}
