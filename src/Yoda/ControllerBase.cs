using System;
using Yoda.Types;

namespace Yoda
{
    public abstract class ControllerBase : IDisposable
    {
        public void Dispose()
        {
            GC.Collect(GC.GetGeneration(this), GCCollectionMode.Forced, true);
        }

        protected IHttpResponse Ok(object value = null)
        {
            return StatusCode(200, value);
        }

        protected IHttpResponse Accepted(object value = null)
        {
            return StatusCode(202, value);
        }

        protected IHttpResponse StatusCode(int statusCode, object value = null)
        {
            return new HttpResponse
            {
                StatusCode = statusCode,
                Value = value,
                ContentType = ContentTypes.JSON,
                ResolverType = ResolverTypes.JSON
            };
        }

        protected IHttpResponse Csv(object value = null)
        {
            return new HttpResponse
            {
                StatusCode = 200,
                Value = value,
                ContentType = ContentTypes.CSV,
                ResolverType = ResolverTypes.CSV
            };
        }

        protected IHttpResponse Content(string value, ContentTypes contentType)
        {
            return new HttpResponse
            {
                StatusCode = 200,
                Value = value,
                ContentType = contentType,
                ResolverType = ResolverTypes.TEXT
            };
        }
    }
}