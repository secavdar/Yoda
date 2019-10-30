using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Reflection;

namespace Yoda.ModelBinders
{
    public interface IModelBinder
    {
        IEnumerable<object> Bind(HttpContext httpContext, ParameterInfo[] parameters);
    }
}