using System;
using System.Threading.Tasks;

namespace ContosoUniversity.Caching
{
    /// <summary>
    /// Non-blocking distributed cache abstraction used by controllers so that views are
    /// rendered from pre-fetched data without synchronous data access.
    /// </summary>
    /// <summary>Serializable projection of Department used for drop-down lists.</summary>
    public class DepartmentLookup
    {
        public int DepartmentID { get; set; }
        public string Name { get; set; }
    }

    public interface IDataCache
    {
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl) where T : class;
        Task RemoveAsync(string key);
    }
}
