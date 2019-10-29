using System;

namespace Yoda.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HttpDeleteAttribute : HttpMethodAttribute
    {
        public HttpDeleteAttribute() : base(new string[] { "DELETE" }) { }
    }
}