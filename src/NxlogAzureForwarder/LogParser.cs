using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace NxlogAzureForwarder
{
    public class LogRecord
    {
        public DateTime EventTimestamp { get; set; }
        public string Origin { get; set; }
        public string RawData { get; set; }
        public Dictionary<string, object> ParsedData { get; set; }
    }

    internal class LogParser
    {
        private readonly static DateTime kEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private class JsonLine
        {
            public string EventUnixTimeMs { get; set; }
            public string EventReceivedTime { get; set; }

            public string DeploymentId { get; set; }
            public string RoleName { get; set; }
            public string RoleInstance { get; set; }

            public string Hostname { get; set; }
        }

        public bool IncludeExtraColumns { get; set; }

        public LogRecord Parse(DateTime receiveTime, string endpoint, string text)
        {
            text = text ?? "";
            JsonLine jsonLine;
            try
            {
                jsonLine = JsonConvert.DeserializeObject<JsonLine>(text);
            }
            catch
            {
                jsonLine = new JsonLine();
            }
            var time = ParseTimestamp(jsonLine, receiveTime);
            var source = ExtractSource(jsonLine, endpoint);

            var record = new LogRecord
            {
                EventTimestamp = ParseTimestamp(jsonLine, receiveTime),
                Origin = ExtractSource(jsonLine, endpoint),
                RawData = text,
            };
            try
            {
                record.ParsedData = JsonConvert.DeserializeObject<Dictionary<string, object>>(text);
            }
            catch { }
            return record;
        }

        private DateTime ParseTimestamp(JsonLine record, DateTime defaultValue)
        {
            try
            {
                var ms = Int64.Parse(record.EventUnixTimeMs);
                return kEpoch + TimeSpan.FromMilliseconds(ms);
            }
            catch { }
            try
            {
                return DateTime.Parse(record.EventReceivedTime).ToUniversalTime();
            }
            catch { }
            return defaultValue;
        }

        private string ExtractSource(JsonLine record, string defaultValue)
        {
            var source = new StringBuilder();
            AppendIfNotBlank(source, record.DeploymentId);
            AppendIfNotBlank(source, record.RoleName);
            AppendIfNotBlank(source, record.RoleInstance);

            if (source.Length == 0) AppendIfNotBlank(source, record.Hostname);

            return source.Length > 0 ? source.ToString() : defaultValue;
        }

        private void AppendIfNotBlank(StringBuilder buffer, string part)
        {
            if (string.IsNullOrWhiteSpace(part)) return;
            if (buffer.Length > 0) buffer.Append("___");
            buffer.Append(part.Trim());
        }
    }
}
