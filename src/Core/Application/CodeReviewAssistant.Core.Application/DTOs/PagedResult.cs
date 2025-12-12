using System;
using System.Collections.Generic;

namespace CodeReviewAssistant.Core.Application.DTOs
{
    public class PagedResult<T>
    {
        public IReadOnlyCollection<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public PagedResult()
        {
            Items = new List<T>();
        }

        public PagedResult(IReadOnlyCollection<T> items, int totalCount, int pageNumber, int pageSize)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
            TotalCount = totalCount;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }

        public static PagedResult<T> Create(IReadOnlyCollection<T> items, int totalCount, int pageNumber, int pageSize)
        {
            return new PagedResult<T>(items, totalCount, pageNumber, pageSize);
        }
    }
}
