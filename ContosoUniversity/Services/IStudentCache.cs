using System.Threading.Tasks;
using ContosoUniversity.Models;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// Non-blocking read-through cache for Student lookups used by view-rendering actions.
    /// Backed by an IDistributedCache (AWS ElastiCache for Redis in cloud environments).
    /// </summary>
    public interface IStudentCache
    {
        Task<Student> GetAsync(int id);
        Task SetAsync(Student student);
        Task RemoveAsync(int id);
    }
}
