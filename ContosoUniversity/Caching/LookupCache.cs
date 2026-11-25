using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using ContosoUniversity.DAL;
using ContosoUniversity.Security;

namespace ContosoUniversity.Caching
{
    /// <summary>
    /// Async, ElastiCache-backed lookups for drop-down data rendered by views.
    /// </summary>
    public static class LookupCache
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

        public static async Task<SelectList> DepartmentsAsync(SchoolContext db, object selectedValue = null)
        {
            var items = await RedisAsyncCache.Instance.GetOrAddAsync("lookup:departments",
                async () => await db.Departments
                    .OrderBy(d => d.DepartmentID)
                    .Select(d => new LookupItem { Value = d.DepartmentID, Text = d.Name })
                    .ToListAsync().ConfigureAwait(false),
                Ttl).ConfigureAwait(false);
            return new SelectList(Sanitize(items), "Value", "Text", selectedValue);
        }

        public static async Task<SelectList> OfficeAssignmentsAsync(SchoolContext db, object selectedValue = null)
        {
            var items = await RedisAsyncCache.Instance.GetOrAddAsync("lookup:officeassignments",
                async () => await db.OfficeAssignments
                    .OrderBy(o => o.InstructorID)
                    .Select(o => new LookupItem { Value = o.InstructorID, Text = o.Location })
                    .ToListAsync().ConfigureAwait(false),
                Ttl).ConfigureAwait(false);
            return new SelectList(Sanitize(items), "Value", "Text", selectedValue);
        }

        // Sanitize free-text labels (after cache retrieval) before they reach the views.
        private static List<LookupItem> Sanitize(IEnumerable<LookupItem> items)
        {
            return (items ?? Enumerable.Empty<LookupItem>())
                .Select(i => new LookupItem { Value = i.Value, Text = HtmlSanitizerProvider.SanitizeForText(i.Text) })
                .ToList();
        }
    }
}
