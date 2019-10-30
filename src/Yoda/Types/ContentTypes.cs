using System.ComponentModel;

namespace Yoda.Types
{
    public enum ContentTypes
    {
        [Description("text/csv")]
        CSV,
        [Description("application/json")]
        JSON
    }
}