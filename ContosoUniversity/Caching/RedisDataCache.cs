using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace ContosoUniversity.Caching
{
    /// <summary>
    /// Azure Cache for Redis backed implementation of <see cref="IDataCache"/>.
    /// The connection string is read from the REDIS_CONNECTION_STRING environment variable
    /// (e.g. "mycache.redis.cache.windows.net:6380,password=...,ssl=True,abortConnect=False").
    /// When it is not configured, or Redis is unavailable, data is loaded directly from the
    /// source (cache-aside with graceful degradation).
    /// </summary>
    public class RedisDataCache : IDataCache
    {
        public const string ConnectionStringVariable = "REDIS_CONNECTION_STRING";

        private static readonly Lazy<ConnectionMultiplexer> Connection = new Lazy<ConnectionMultiplexer>(() =>
        {
            var cs = Environment.GetEnvironmentVariable(ConnectionStringVariable);
            if (string.IsNullOrWhiteSpace(cs))
            {
                return null;
            }
            try
            {
                return ConnectionMultiplexer.Connect(cs);
            }
            catch (Exception)
            {
                return null;
            }
        });

        private static IDatabase GetDatabase()
        {
            var conn = Connection.Value;
            return (conn != null && conn.IsConnected) ? conn.GetDatabase() : null;
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl) where T : class
        {
            var db = GetDatabase();
            if (db != null)
            {
                try
                {
                    var cached = await db.StringGetAsync(key).ConfigureAwait(false);
                    if (cached.HasValue)
                    {
                        return JsonConvert.DeserializeObject<T>((string)cached);
                    }
                }
                catch (Exception)
                {
                    // Cache failure must never break the request; fall back to the data source.
                }
            }

            var value = await factory().ConfigureAwait(false);

            if (db != null && value != null)
            {
                try
                {
                    await db.StringSetAsync(key, JsonConvert.SerializeObject(value), ttl).ConfigureAwait(false);
                }
                catch (Exception)
                {
                }
            }
            return value;
        }

        public async Task RemoveAsync(string key)
        {
            var db = GetDatabase();
            if (db == null)
            {
                return;
            }
            try
            {
                await db.KeyDeleteAsync(key).ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
        }
    }
}
