using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace NxlogAzureForwarder
{
    internal static class PropertyRender
    {
        private static readonly Regex _kPropertyParser;

        static PropertyRender()
        {
            _kPropertyParser = new Regex(@"(?<start>[\$~]\{)(?<property>[^:}]+)(?<format>:[^}]+)?(?<end>\})",
                RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        }

        private static string Inverted(string text)
        {
            var buffer = new StringBuilder();
            foreach (var ch in text)
            {
                if (ch >= '0' && ch <= '9')
                {
                    buffer.Append((char)('9' - (ch - '0')));
                }
                else if (ch >= 'A' && ch <= 'Z')
                {
                    buffer.Append((char)('Z' - (ch - 'A')));
                }
                else if (ch >= 'a' && ch <= 'z')
                {
                    buffer.Append((char)('z' - (ch - 'a')));
                }
                else
                {
                    buffer.Append(ch);
                }
            }
            return buffer.ToString();
        }

        public static string Render(string format, LogRecord record)
        {
            return _kPropertyParser.Replace(format, delegate(Match m)
            {
                Group startGroup = m.Groups["start"];
                Func<string, string> transform = (x) => x;
                if (startGroup.Value.StartsWith("~")) transform = Inverted;

                Group propertyGroup = m.Groups["property"];
                var value = record.Resolve(propertyGroup.Value ?? "");

                Group formatGroup = m.Groups["format"];
                var singleFormat = "{0" + formatGroup.Value + "}";

                return transform(string.Format(CultureInfo.InvariantCulture, singleFormat, value));
            });
        }
    }
}
