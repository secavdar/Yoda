using System;

namespace Yoda.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HttpHeadAttribute : HttpMethodAttribute
    {
        public HttpHeadAttribute() : base(new string[] { "HEAD" }) { }
    }
}