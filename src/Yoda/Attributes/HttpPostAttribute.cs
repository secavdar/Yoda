using System;

namespace Yoda.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HttpPostAttribute : HttpMethodAttribute
    {
        public HttpPostAttribute() : base(new string[] { "POST" }) { }
    }
}