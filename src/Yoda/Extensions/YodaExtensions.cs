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
            var httpResponseType = typeof(IHttpResponse);
            var controllerBaseType = typeof(ControllerBase);
            var controllers = assembly.GetTypes().Where(x => controllerBaseType.IsAssignableFrom(x) && !x.IsAbstract).ToArray();

            foreach (var controllerType in controllers)
            {
                var methods = controllerType.GetMembers()
                    .Where(x => x.MemberType == MemberTypes.Method && x.DeclaringType == controllerType)
                    .Cast<MethodInfo>()
                    .Where(x => httpResponseType.IsAssignableFrom(x.ReturnType) || (taskType.IsAssignableFrom(x.ReturnType) && x.ReturnType.IsGenericType && x.ReturnType.GenericTypeArguments.Count() == 1 && x.ReturnType.GenericTypeArguments.Any(y => httpResponseType.IsAssignableFrom(y))))
                    .ToArray();

                var classRouteAttributes = controllerType.GetCustomAttributes<RouteAttribute>();

                foreach (var method in methods)
                {
                    var routeAttributes = method.GetCustomAttributes<RouteAttribute>();
                    var allowedHttpMethods = method.GetCustomAttributes<HttpMethodAttribute>().SelectMany(x => x.HttpMethods).Distinct().ToArray();

                    var attributes = classRouteAttributes.Concat(routeAttributes);

                    var route = string.Join("/", attributes.Select(x => x.Template));

                    if (!route.StartsWith("/"))
                        route = "/" + route;

                    app.Map(route, builder =>
                    {
                        builder.Run(async (context) =>
                        {
                            if (!allowedHttpMethods.Contains(context.Request.Method))
                                return;

                            var controllerArguments = GetDependencies(app, controllerType).ToArray();
                            var controller = Activator.CreateInstance(controllerType, controllerArguments);

                            using (var reader = new StreamReader(context.Request.Body))
                            {
                                var requestBody = await reader.ReadToEndAsync();

                                var parameters = method.GetParameters();
                                var arguments = ResolveParameters(requestBody, parameters, context.Request.Query).ToArray();

                                var result = taskType.IsAssignableFrom(method.ReturnType)
                                           ? await (Task<IHttpResponse>)method.Invoke(controller, arguments)
                                           : (IHttpResponse)method.Invoke(controller, arguments);

                                if (result == null)
                                    result = new HttpResponse
                                    {
                                        StatusCode = 200
                                    };

                                context.Response.Headers.Add("Content-Type", "applcation/json");
                                context.Response.StatusCode = result.StatusCode;

                                if (result.Value != null)
                                {
                                    var json = JsonConvert.SerializeObject(result.Value);
                                    await context.Response.WriteAsync(json);
                                }
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
            var input = string.IsNullOrWhiteSpace(requestBody) ? null : JObject.Parse(requestBody);

            foreach (var parameter in parameters)
            {
                if (query.ContainsKey(parameter.Name))
                    yield return query[parameter.Name].ToString();
                else
                {
                    if (parameter.ParameterType.IsPrimitive || parameter.ParameterType.Equals(typeof(string)))
                    {
                        if (input != null)
                            yield return ((JValue)input[parameter.Name])?.Value;
                        else
                            yield return Activator.CreateInstance(parameter.ParameterType);
                    }
                    else
                        yield return JsonConvert.DeserializeObject(requestBody, parameter.ParameterType);
                }
            }
        }

        private static IEnumerable<object> GetDependencies(IApplicationBuilder app, Type controllerType)
        {
            var ctor = controllerType.GetConstructors().FirstOrDefault();
            var ctorParameters = ctor.GetParameters();

            foreach (var item in ctorParameters)
                yield return app.ApplicationServices.GetService(item.ParameterType);
        }
    }
}