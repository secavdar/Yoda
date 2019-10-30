using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Yoda.ModelBinders
{
    public class ModelBinder : IModelBinder
    {
        public IEnumerable<object> Bind(HttpContext httpContext, ParameterInfo[] parameters)
        {
            var query = httpContext.Request.Query;

            using (var reader = new StreamReader(httpContext.Request.Body))
            {
                var requestBody = reader.ReadToEnd();

                var input = string.IsNullOrWhiteSpace(requestBody) ? null : JObject.Parse(requestBody);

                foreach (var parameter in parameters)
                {
                    if (httpContext.Items.ContainsKey(parameter.Name))
                        yield return Convert.ChangeType(httpContext.Items[parameter.Name], parameter.ParameterType);
                    else if (query.ContainsKey(parameter.Name))
                        yield return Convert.ChangeType(query[parameter.Name], parameter.ParameterType);
                    else
                    {
                        if (parameter.ParameterType.IsPrimitive || parameter.ParameterType.Equals(typeof(string)))
                        {
                            if (input != null)
                                yield return Convert.ChangeType(((JValue)input[parameter.Name])?.Value, parameter.ParameterType);
                            else
                                yield return Activator.CreateInstance(parameter.ParameterType);
                        }
                        else
                            yield return JsonConvert.DeserializeObject(requestBody, parameter.ParameterType);
                    }
                }
            }
        }
    }
}