namespace CustomerService.DTOs
{
    public class PaginatedResponse<T>
    {
        public IReadOnlyList<T> Data { get; set; } = Array.Empty<T>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
    }
}
