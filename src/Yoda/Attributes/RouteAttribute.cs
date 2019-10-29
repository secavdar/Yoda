using System;

namespace Yoda.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RouteAttribute : Attribute
    {
        public RouteAttribute(string template)
        {
            Template = template;
        }

        public string Template { get; }
    }
}