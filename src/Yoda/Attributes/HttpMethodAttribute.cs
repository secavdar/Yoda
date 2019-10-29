using System;
using System.Collections.Generic;

namespace Yoda.Attributes
{
    public class HttpMethodAttribute : Attribute
    {
        public HttpMethodAttribute(IEnumerable<string> httpMethods)
        {
            HttpMethods = httpMethods;
        }

        public IEnumerable<string> HttpMethods { get; }
    }
}