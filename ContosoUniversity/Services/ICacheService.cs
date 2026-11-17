using System;
using System.Threading.Tasks;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// Non-blocking cache abstraction backed by AWS ElastiCache (Redis).
    /// </summary>
    public interface ICacheService
    {
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null) where T : class;
        Task RemoveAsync(string key);
    }
}
