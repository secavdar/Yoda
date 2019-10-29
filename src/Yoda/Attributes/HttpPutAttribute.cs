using System;

namespace Yoda.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HttpPutAttribute : HttpMethodAttribute
    {
        public HttpPutAttribute() : base(new string[] { "PUT" }) { }
    }
}