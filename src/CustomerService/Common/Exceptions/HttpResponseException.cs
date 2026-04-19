namespace CustomerService.Common.Exceptions
{
    public class HttpResponseException : Exception
    {
        public HttpResponseException(int statusCode, string message, string? responseBody = null)
            : base(message)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }

        public int StatusCode { get; }

        public string? ResponseBody { get; }
    }
}
