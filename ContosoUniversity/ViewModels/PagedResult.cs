using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ContosoUniversity.ViewModels
{
    public class PagedResult<T> : IEnumerable<T>
    {
        private readonly List<T> _items;

        public PagedResult(IQueryable<T> source, int pageNumber, int pageSize)
        {
            int total = source.Count();
            PageCount = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
            PageNumber = pageNumber < 1 ? 1 : pageNumber;
            _items = source.Skip((PageNumber - 1) * pageSize).Take(pageSize).ToList();
        }

        public int PageNumber { get; }
        public int PageCount { get; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < PageCount;

        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
