using Yoda.Types;

namespace Yoda
{
    public interface IHttpResponse
    {
        int StatusCode { get; set; }
        object Value { get; set; }
        ContentTypes ContentType { get; set; }
        string FormatterType { get; set; }
    }
}