namespace Yoda
{
    public interface IHttpResponse
    {
        int StatusCode { get; set; }
        object Value { get; set; }
    }
}