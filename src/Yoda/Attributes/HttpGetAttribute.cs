using System;

namespace Yoda.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HttpGetAttribute : HttpMethodAttribute
    {
        public HttpGetAttribute() : base(new string[] { "GET" }) { }
    }
}