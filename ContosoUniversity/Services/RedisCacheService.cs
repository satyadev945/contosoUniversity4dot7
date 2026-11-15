using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace ContosoUniversity.Services
{
    public interface ICacheService
    {
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> loader, TimeSpan ttl) where T : class;
    }

    /// <summary>
    /// Non-blocking distributed cache backed by GCP Memorystore (Redis).
    /// The endpoint is read from the REDIS_CONNECTION_STRING environment variable
    /// (e.g. "10.0.0.3:6379"). When it is not configured, or Redis is unavailable,
    /// the loader is invoked directly so the application keeps working.
    /// </summary>
    public class RedisCacheService : ICacheService
    {
        private static readonly Lazy<ConnectionMultiplexer> Connection = new Lazy<ConnectionMultiplexer>(() =>
        {
            var cs = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(cs))
            {
                return null;
            }
            var options = ConfigurationOptions.Parse(cs);
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 2000;
            options.SyncTimeout = 2000;
            return ConnectionMultiplexer.Connect(options);
        });

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> loader, TimeSpan ttl) where T : class
        {
            IDatabase redis = null;
            try
            {
                var mux = Connection.Value;
                if (mux != null && mux.IsConnected)
                {
                    redis = mux.GetDatabase();
                    var cached = await redis.StringGetAsync(key).ConfigureAwait(false);
                    if (cached.HasValue)
                    {
                        return JsonConvert.DeserializeObject<T>((string)cached);
                    }
                }
            }
            catch (Exception)
            {
                redis = null; // cache failure must never break the request
            }

            var value = await loader().ConfigureAwait(false);

            if (redis != null && value != null)
            {
                try
                {
                    await redis.StringSetAsync(key, JsonConvert.SerializeObject(value), ttl).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // ignore cache write failures
                }
            }
            return value;
        }
    }
}
