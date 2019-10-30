using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Yoda.Formatters
{
    public class JsonOutputFormatter : IOutputFormatter
    {
        public string FormatterType => "JSON";

        public async Task ResolveAsync(HttpContext httpContext, IHttpResponse httpResponse)
        {
            var json = JsonConvert.SerializeObject(httpResponse.Value);
            await httpContext.Response.WriteAsync(json);
        }
    }
}