using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NxlogAzureForwarder
{
    public class LogRecord
    {
        public DateTime Timestamp { get; set; }
        public string Origin { get; set; }
        public string RawData { get; set; }
        public Dictionary<string, object> ParsedData { get; set; }
    }

    public static class PropertyRender
    {
        private static readonly Regex _kPropertyParser;

        static PropertyRender()
        {
            _kPropertyParser = new Regex(@"(?<start>\$\{)(?<property>[\w]+)(?<format>:[^}]+)?(?<end>\})",
                RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        }

        private static object Resolve(LogRecord record, string property)
        {
            switch (property)
            {
                case "Origin":
                    return record.Origin;
                case "Timestamp":
                    return record.Timestamp;
                case "RawData":
                    return record.RawData;
            }
            try
            {
                return record.ParsedData[property];
            }
            catch { }
            return null;
        }

        public static string Render(string format, LogRecord record)
        {
            return _kPropertyParser.Replace(format, delegate(Match m)
            {
                Group startGroup = m.Groups["start"];
                Group propertyGroup = m.Groups["property"];
                Group formatGroup = m.Groups["format"];
                Group endGroup = m.Groups["end"];
                var value = Resolve(record, propertyGroup.Value ?? "");
                var singleFormat = "{0" + formatGroup.Value + "}";
                return string.Format(CultureInfo.InvariantCulture, singleFormat, value);
            });
        }
    }
}
