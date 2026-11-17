using System;
using System.Threading.Tasks;
using ContosoUniversity.Models;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// Non-blocking cache-aside layer for Student read models rendered by Razor views.
    /// Backed by IDistributedCache (Azure Cache for Redis when REDIS_CONNECTION_STRING is set).
    /// Cache failures never break a request; callers fall back to the database.
    /// </summary>
    public interface IStudentCache
    {
        Task<Student> GetAsync(int id);
        Task SetAsync(Student student);
        Task RemoveAsync(int id);
    }

    public class StudentCache : IStudentCache
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        private static readonly DistributedCacheEntryOptions EntryOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

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
                return json == null ? null : JsonConvert.DeserializeObject<Student>(json, JsonSettings);
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
                var json = JsonConvert.SerializeObject(student, JsonSettings);
                await _cache.SetStringAsync(Key(student.ID), json, EntryOptions).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // cache is best-effort
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
                // cache is best-effort
            }
        }
    }
}
