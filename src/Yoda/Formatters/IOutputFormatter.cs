using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Yoda.Formatters
{
    public interface IOutputFormatter
    {
        string FormatterType { get; }

        Task ResolveAsync(HttpContext httpContext, IHttpResponse httpResponse);
    }
}