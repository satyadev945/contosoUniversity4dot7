using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace ContosoUniversity.Caching
{
    /// <summary>
    /// GCP Memorystore (Redis) cache. The endpoint is read from the REDIS_CONNECTION_STRING
    /// environment variable (e.g. "10.0.0.3:6379,abortConnect=false") or the
    /// "RedisConnectionString" app setting. When no endpoint is configured or Redis is
    /// unavailable, the cache fails open so requests are served directly from the database.
    /// </summary>
    public class RedisCacheService : ICacheService
    {
        private static readonly Lazy<ConnectionMultiplexer> Connection = new Lazy<ConnectionMultiplexer>(() =>
        {
            var cs = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING")
                     ?? ConfigurationManager.AppSettings["RedisConnectionString"];
            if (string.IsNullOrWhiteSpace(cs))
            {
                return null;
            }
            var options = ConfigurationOptions.Parse(cs);
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);
        });

        private static IDatabase Db
        {
            get
            {
                try
                {
                    var conn = Connection.Value;
                    return conn == null ? null : conn.GetDatabase();
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Redis unavailable: " + ex.Message);
                    return null;
                }
            }
        }

        public async Task<T> GetAsync<T>(string key) where T : class
        {
            var db = Db;
            if (db == null) return null;
            try
            {
                string json = await db.StringGetAsync(key).ConfigureAwait(false);
                return json == null ? null : JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Redis get failed: " + ex.Message);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
        {
            var db = Db;
            if (db == null) return;
            try
            {
                await db.StringSetAsync(key, JsonConvert.SerializeObject(value), expiry ?? TimeSpan.FromMinutes(5))
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Redis set failed: " + ex.Message);
            }
        }

        public async Task<long> IncrementAsync(string key)
        {
            var db = Db;
            if (db == null) return 0;
            try
            {
                return await db.StringIncrementAsync(key).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Redis increment failed: " + ex.Message);
                return 0;
            }
        }

        public async Task<string> GetStringAsync(string key)
        {
            var db = Db;
            if (db == null) return null;
            try
            {
                return await db.StringGetAsync(key).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Redis get failed: " + ex.Message);
                return null;
            }
        }
    }
}
