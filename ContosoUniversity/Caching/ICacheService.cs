using System;
using System.Threading.Tasks;

namespace ContosoUniversity.Caching
{
    /// <summary>
    /// Non-blocking cache abstraction backed by AWS ElastiCache (Redis).
    /// </summary>
    public interface ICacheService
    {
        Task<T> GetAsync<T>(string key) where T : class;

        Task SetAsync<T>(string key, T value, TimeSpan ttl) where T : class;

        Task RemoveAsync(string key);
    }
}
