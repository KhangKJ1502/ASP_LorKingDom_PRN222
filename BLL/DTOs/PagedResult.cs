using System.Collections.Generic;

namespace BLL.DTOs
{
    public class PagedResult<T>
    {
        public IList<T> Items { get; set; } = new List<T>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        // convenience cho View
        public int TotalPages => PageSize > 0
            ? (int)((Total + PageSize - 1) / PageSize)
            : 1;

        public bool HasPrev => Page > 1;
        public bool HasNext => Page < TotalPages;
    }
}
