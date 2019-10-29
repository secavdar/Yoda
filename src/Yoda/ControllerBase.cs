using System;

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
            return new HttpResponse
            {
                StatusCode = 200,
                Value = value
            };
        }

        protected IHttpResponse StatusCode(int statusCode, object value = null)
        {
            return new HttpResponse
            {
                StatusCode = statusCode,
                Value = value
            };
        }
    }
}