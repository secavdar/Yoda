﻿namespace Yoda
{
    public class HttpResponse : IHttpResponse
    {
        public int StatusCode { get; set; }
        public object Value { get; set; }
    }
}