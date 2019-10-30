using Yoda.Formatters;

namespace Yoda.Options
{
    public class YodaOptions
    {
        public IOutputFormatter CsvOutputFormatter { get; set; }
        public IOutputFormatter JsonOutputFormatter { get; set; }
        public IOutputFormatter TextOutputFormatter { get; set; }
    }
}