using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Yoda.Attributes;

namespace Yoda.Extensions
{
    public static class YodaExtensions
    {
        public static IApplicationBuilder UseYoda(this IApplicationBuilder app)
        {
            var assembly = Assembly.GetCallingAssembly();
            var taskType = typeof(Task);
            var controllerBaseType = typeof(ControllerBase);
            var controllers = assembly.GetTypes().Where(x => controllerBaseType.IsAssignableFrom(x) && !x.IsAbstract).ToArray();

            foreach (var controllerType in controllers)
            {
                var methods = controllerType.GetMembers().Where(x => x.MemberType == MemberTypes.Method && x.DeclaringType == controllerType).ToArray();

                foreach (MethodInfo method in methods)
                {
                    var routeAttribute = method.GetCustomAttribute<RouteAttribute>();
                    var allowedHttpMethods = method.GetCustomAttributes<HttpMethodAttribute>().SelectMany(x => x.HttpMethods).Distinct().ToArray();

                    var route = routeAttribute.Template;

                    if (!route.StartsWith("/"))
                        route = "/" + route;

                    app.Map(route, builder =>
                    {
                        builder.Run(async (context) =>
                        {
                            if (!allowedHttpMethods.Contains(context.Request.Method))
                                return;

                            var controller = Activator.CreateInstance(controllerType);

                            using (var reader = new StreamReader(context.Request.Body))
                            {
                                var requestBody = await reader.ReadToEndAsync();

                                var parameters = method.GetParameters();
                                var arguments = ResolveParameters(requestBody, parameters, context.Request.Query).ToArray();

                                var result = taskType.IsAssignableFrom(method.ReturnType)
                                           ? await (Task<IHttpResponse>)method.Invoke(controller, arguments)
                                           : (IHttpResponse)method.Invoke(controller, arguments);

                                var json = JsonConvert.SerializeObject(result.Value);

                                context.Response.Headers.Add("Content-Type", "applcation/json");
                                context.Response.StatusCode = result.StatusCode;

                                await context.Response.WriteAsync(json);
                            }

                            ((IDisposable)controller).Dispose();
                        });
                    });
                }
            }

            return app;
        }

        private static IEnumerable<object> ResolveParameters(string requestBody, ParameterInfo[] parameters, IQueryCollection query)
        {
            var input = JObject.Parse(requestBody);

            foreach (var parameter in parameters)
            {
                if (query.ContainsKey(parameter.Name))
                    yield return query[parameter.Name].ToString();
                else
                {
                    if (parameter.ParameterType.IsPrimitive || parameter.ParameterType.Equals(typeof(string)))
                        yield return ((JValue)input[parameter.Name])?.Value;
                    else
                        yield return JsonConvert.DeserializeObject(requestBody, parameter.ParameterType);
                }
            }
        }
    }
}