using Yoda.Types;

namespace Yoda
{
    internal class HttpResponse : IHttpResponse
    {
        public int StatusCode { get; set; }
        public object Value { get; set; }
        public ContentTypes ContentType { get; set; }
        public ResolverTypes ResolverType { get; set; }
    }
}