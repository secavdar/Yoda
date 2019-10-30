using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Yoda.Attributes;
using Yoda.Types;

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

                    app.MapWhen(context => 
                    {
                        if (!allowedHttpMethods.Contains(context.Request.Method))
                            return false;

                        var requestUrl = context.Request.Path.Value;
                        var questionMarkIndex = requestUrl.IndexOf('?');

                        if (questionMarkIndex != -1)
                            requestUrl = requestUrl.Remove(questionMarkIndex);

                        var routeSplit = route.Split('/', StringSplitOptions.RemoveEmptyEntries);
                        var requestUrlSplit = requestUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);

                        if (routeSplit.Length != requestUrlSplit.Length)
                            return false;

                        for (int i = 0; i < routeSplit.Length; i++)
                            if (routeSplit[i].StartsWith('{') && routeSplit[i].EndsWith('}'))
                                context.Items.Add(routeSplit[i].Substring(1, routeSplit[i].Length - 2), requestUrlSplit[i]);

                        return true;
                    }, builder =>
                    {
                        builder.Run(async (context) =>
                        {
                            var controllerArguments = GetDependencies(app, controllerType).ToArray();
                            var controller = Activator.CreateInstance(controllerType, controllerArguments);

                            var parameters = method.GetParameters();
                            var arguments = ResolveParameters(context, parameters, context.Request.Query).ToArray();

                            var result = taskType.IsAssignableFrom(method.ReturnType)
                                        ? await (Task<IHttpResponse>)method.Invoke(controller, arguments)
                                        : (IHttpResponse)method.Invoke(controller, arguments);

                            if (result == null)
                                result = new HttpResponse
                                {
                                    StatusCode = 200
                                };


                            context.Response.Headers.Add("Content-Type", result.ContentType.ToDescription());
                            context.Response.StatusCode = result.StatusCode;

                            if (result.Value != null)
                            {
                                switch (result.ResolverType)
                                {
                                    case ResolverTypes.CSV:

                                        var stringBuilder = new StringBuilder();

                                        if (typeof(IEnumerable).IsAssignableFrom(result.Value.GetType()))
                                        {
                                            bool header = true;
                                            foreach (var o in ((IEnumerable)result.Value))
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
                                            var properties = result.Value.GetType().GetProperties();

                                            stringBuilder.AppendLine(string.Join(",", properties.Select(p => p.Name)) + "\n");
                                            stringBuilder.AppendLine(string.Join(",", properties.Select(p => $"{ (p.GetValue(result.Value) is string ? "\"" : "") }{ p.GetValue(result.Value) ?? "" }{ (p.GetValue(result.Value) is string ? "\"" : "") }")));
                                        }

                                        await context.Response.WriteAsync(stringBuilder.ToString());

                                        break;
                                    case ResolverTypes.JSON:
                                        var json = JsonConvert.SerializeObject(result.Value);
                                        await context.Response.WriteAsync(json);
                                        break;
                                    case ResolverTypes.TEXT:
                                        await context.Response.WriteAsync(result.Value.ToString());
                                        break;
                                }
                            }

                            ((IDisposable)controller).Dispose();
                        });
                    });
                }
            }

            return app;
        }

        private static IEnumerable<object> ResolveParameters(HttpContext context, ParameterInfo[] parameters, IQueryCollection query)
        {
            using (var reader = new StreamReader(context.Request.Body))
            {
                var requestBody = reader.ReadToEnd();

                var input = string.IsNullOrWhiteSpace(requestBody) ? null : JObject.Parse(requestBody);

                foreach (var parameter in parameters)
                {
                    if (context.Items.ContainsKey(parameter.Name))
                        yield return Convert.ChangeType(context.Items[parameter.Name], parameter.ParameterType);
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

        private static IEnumerable<object> GetDependencies(IApplicationBuilder app, Type controllerType)
        {
            var ctor = controllerType.GetConstructors().FirstOrDefault();
            var ctorParameters = ctor.GetParameters();

            foreach (var item in ctorParameters)
                yield return app.ApplicationServices.GetService(item.ParameterType);
        }

        private static string ToDescription<T>(this T source)
        {
            var fi = source.GetType().GetField(source.ToString());
            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0) 
                return attributes[0].Description;
            else 
                return source.ToString();
        }
    }
}