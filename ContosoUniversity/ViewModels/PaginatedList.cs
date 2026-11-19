using System;
using System.Collections.Generic;
using System.Linq;

namespace ContosoUniversity.ViewModels
{
    // Simple paging helper (replaces the System.Web-based PagedList packages)
    public class PaginatedList<T> : List<T>
    {
        public int PageNumber { get; private set; }
        public int PageCount { get; private set; }

        public bool HasPreviousPage
        {
            get { return PageNumber > 1; }
        }

        public bool HasNextPage
        {
            get { return PageNumber < PageCount; }
        }

        public PaginatedList(List<T> items, int count, int pageNumber, int pageSize)
        {
            PageNumber = pageNumber;
            PageCount = (int)Math.Ceiling(count / (double)pageSize);
            AddRange(items);
        }

        // source must be ordered
        public static PaginatedList<T> Create(IQueryable<T> source, int pageNumber, int pageSize)
        {
            var count = source.Count();
            var items = source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            return new PaginatedList<T>(items, count, pageNumber, pageSize);
        }
    }
}
