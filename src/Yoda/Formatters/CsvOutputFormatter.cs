using Microsoft.AspNetCore.Http;
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yoda.Formatters
{
    public class CsvOutputFormatter : IOutputFormatter
    {
        public string FormatterType => "CSV";

        public async Task ResolveAsync(HttpContext httpContext, IHttpResponse httpResponse)
        {
            var stringBuilder = new StringBuilder();

            if (typeof(IEnumerable).IsAssignableFrom(httpResponse.Value.GetType()))
            {
                bool header = true;
                foreach (var o in ((IEnumerable)httpResponse.Value))
                {
                    var objectProperties = o.GetType().GetProperties();

                    if (header)
                        stringBuilder.AppendLine(string.Join(",", objectProperties.Select(p => p.Name)));

                    header = false;
                    stringBuilder.AppendLine(string.Join(",", objectProperties.Select(p =>
                    {
                        var val = p.GetValue(o);
                        string item = (val == null ? "" : val.ToString());

                        if (val is string)
                            item = "\"" + val + "\"";

                        if (val is DateTime)
                            item = ((DateTime)val).ToString("yyyMMdd", new CultureInfo("en-GB"));

                        return item;
                    })));
                }
            }
            else
            {
                var properties = httpResponse.Value.GetType().GetProperties();

                stringBuilder.AppendLine(string.Join(",", properties.Select(p => p.Name)) + "\n");
                stringBuilder.AppendLine(string.Join(",", properties.Select(p => $"{ (p.GetValue(httpResponse.Value) is string ? "\"" : "") }{ p.GetValue(httpResponse.Value) ?? "" }{ (p.GetValue(httpResponse.Value) is string ? "\"" : "") }")));
            }

            await httpContext.Response.WriteAsync(stringBuilder.ToString());
        }
    }
}