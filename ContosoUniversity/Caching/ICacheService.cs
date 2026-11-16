using System;
using System.Threading.Tasks;

namespace ContosoUniversity.Caching
{
    /// <summary>
    /// Non-blocking distributed cache abstraction (backed by GCP Memorystore for Redis).
    /// </summary>
    public interface ICacheService
    {
        Task<T> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
        Task<long> IncrementAsync(string key);
        Task<string> GetStringAsync(string key);
    }
}
