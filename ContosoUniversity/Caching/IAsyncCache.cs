using System;
using System.Threading.Tasks;

namespace ContosoUniversity.Caching
{
    /// <summary>
    /// Non-blocking cache abstraction used by controllers to retrieve read-mostly data
    /// without blocking request threads during view rendering.
    /// </summary>
    public interface IAsyncCache
    {
        Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl) where T : class;
    }
}
