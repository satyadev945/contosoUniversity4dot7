using System;
using System.Collections.Generic;
using System.Linq;

namespace ContosoUniversity
{
    // Replacement for PagedList.IPagedList (not available for ASP.NET Core)
    public class PaginatedList<T> : List<T>
    {
        public int PageNumber { get; private set; }
        public int PageCount { get; private set; }

        public PaginatedList(List<T> items, int count, int pageNumber, int pageSize)
        {
            PageNumber = pageNumber;
            PageCount = (int)Math.Ceiling(count / (double)pageSize);
            AddRange(items);
        }

        public bool HasPreviousPage => PageNumber > 1;

        public bool HasNextPage => PageNumber < PageCount;

        public static PaginatedList<T> Create(IQueryable<T> source, int pageNumber, int pageSize)
        {
            var count = source.Count();
            var items = source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            return new PaginatedList<T>(items, count, pageNumber, pageSize);
        }
    }
}
