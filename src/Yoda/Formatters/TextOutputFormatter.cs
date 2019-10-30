using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Yoda.Formatters
{
    public class TextOutputFormatter : IOutputFormatter
    {
        public string FormatterType => "TEXT";

        public async Task ResolveAsync(HttpContext httpContext, IHttpResponse httpResponse)
        {
            await httpContext.Response.WriteAsync(httpResponse.Value.ToString());
        }
    }
}