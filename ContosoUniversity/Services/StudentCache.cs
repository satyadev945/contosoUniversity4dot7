using System;
using System.Threading.Tasks;
using ContosoUniversity.Models;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// IDistributedCache (AWS ElastiCache Redis) backed student cache. All operations are async/non-blocking.
    /// Cache failures never break the request; callers fall back to the database.
    /// Only scalar student properties are cached (no navigation properties) to avoid object cycles.
    /// </summary>
    public class StudentCache : IStudentCache
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);
        private readonly IDistributedCache _cache;

        public StudentCache(IDistributedCache cache)
        {
            _cache = cache;
        }

        private static string Key(int id)
        {
            return "student:" + id;
        }

        public async Task<Student> GetAsync(int id)
        {
            try
            {
                var json = await _cache.GetStringAsync(Key(id)).ConfigureAwait(false);
                return string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject<Student>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task SetAsync(Student student)
        {
            if (student == null) return;
            try
            {
                var flat = new Student
                {
                    ID = student.ID,
                    LastName = student.LastName,
                    FirstMidName = student.FirstMidName,
                    EnrollmentDate = student.EnrollmentDate
                };
                await _cache.SetStringAsync(
                    Key(student.ID),
                    JsonConvert.SerializeObject(flat),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl }).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Cache is best-effort.
            }
        }

        public async Task RemoveAsync(int id)
        {
            try
            {
                await _cache.RemoveAsync(Key(id)).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Cache is best-effort.
            }
        }
    }
}
