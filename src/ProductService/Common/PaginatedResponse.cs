using System;
using System.Collections.Generic;

namespace ProductService.Common
{
    public class PaginatedResponse<T>
    {
        public IEnumerable<T> Data { get; set; } = new List<T>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages => (PageSize > 0) ? (int)Math.Ceiling((double)TotalRecords / PageSize) : 0;
    }
}
