using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Yoda.Attributes;
using Yoda.Formatters;
using Yoda.ModelBinders;
using Yoda.Options;

namespace Yoda.Extensions
{
    public static class YodaExtensions
    {
        public static IServiceCollection AddYoda(this IServiceCollection services, Action<YodaOptions> setupAction = null)
        {
            var options = new YodaOptions();
            setupAction?.Invoke(options);

            services.AddSingleton<IOutputFormatter>(options.CsvOutputFormatter ?? new CsvOutputFormatter());
            services.AddSingleton<IOutputFormatter>(options.JsonOutputFormatter ?? new JsonOutputFormatter());
            services.AddSingleton<IOutputFormatter>(options.TextOutputFormatter ?? new TextOutputFormatter());

            services.AddSingleton<IModelBinder, ModelBinder>();

            return services;
        }

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
                    .Where(x => httpResponseType.IsAssignableFrom(x.ReturnType) || 
                                (
                                    taskType.IsAssignableFrom(x.ReturnType) && 
                                    x.ReturnType.IsGenericType && 
                                    x.ReturnType.GenericTypeArguments.Count() == 1 && 
                                    x.ReturnType.GenericTypeArguments.Any(y => httpResponseType.IsAssignableFrom(y))
                                ))
                    .ToArray();

                var classRouteAttributes = controllerType.GetCustomAttributes<RouteAttribute>();

                foreach (var method in methods)
                {
                    var routeAttributes = method.GetCustomAttributes<RouteAttribute>();
                    var allowedHttpMethods = method.GetCustomAttributes<HttpMethodAttribute>()
                        .SelectMany(x => x.HttpMethods)
                        .Distinct()
                        .ToArray();

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
                            var scopeFactory = builder.ApplicationServices.GetService<IServiceScopeFactory>();

                            using (var scope = scopeFactory.CreateScope())
                            {
                                var controllerArguments = GetDependencies(scope, controllerType).ToArray();
                                var controller = Activator.CreateInstance(controllerType, controllerArguments);

                                var modelBinder = scope.ServiceProvider.GetService<IModelBinder>();
                                var arguments = modelBinder.Bind(context, method.GetParameters()).ToArray();

                                var httpResponse = taskType.IsAssignableFrom(method.ReturnType)
                                                 ? await (Task<IHttpResponse>)method.Invoke(controller, arguments)
                                                 : (IHttpResponse)method.Invoke(controller, arguments);

                                if (httpResponse == null)
                                    httpResponse = new HttpResponse
                                    {
                                        StatusCode = 200
                                    };


                                context.Response.Headers.Add("Content-Type", httpResponse.ContentType.ToDescription());
                                context.Response.StatusCode = httpResponse.StatusCode;

                                if (httpResponse.Value != null)
                                {
                                    var outputFormatters = scope.ServiceProvider.GetServices<IOutputFormatter>();
                                    var outputFormatter = outputFormatters.FirstOrDefault(x => x.FormatterType == httpResponse.FormatterType);

                                    await outputFormatter.ResolveAsync(context, httpResponse);
                                }

                                ((IDisposable)controller).Dispose();
                            }
                        });
                    });
                }
            }

            return app;
        }

        private static IEnumerable<object> GetDependencies(IServiceScope scope, Type controllerType)
        {
            var ctor = controllerType.GetConstructors().FirstOrDefault();
            var ctorParameters = ctor.GetParameters();

            foreach (var item in ctorParameters)
                yield return scope.ServiceProvider.GetService(item.ParameterType);
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