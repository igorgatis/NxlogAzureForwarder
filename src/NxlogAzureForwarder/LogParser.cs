using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace NxlogAzureForwarder
{
    internal class LogRecord
    {
        public DateTime Timestamp { get; set; }
        public string Origin { get; set; }
        public string RawData { get; set; }
        public Dictionary<string, string> ParsedProperties { get; set; }
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

            public string SourceModuleName { get; set; }
            public string SourceName { get; set; }
            public string Severity { get; set; }
            public string Message { get; set; }
        }

        public bool IncludeExtraColumns { get; set; }

        public LogRecord Parse(DateTime receiveTime, string endpoint, string text)
        {
            text = text ?? "";
            JsonLine record;
            try
            {
                record = JsonConvert.DeserializeObject<JsonLine>(text);
            }
            catch
            {
                record = new JsonLine();
            }
            var time = ParseTimestamp(record, receiveTime);
            var source = ExtractSource(record, endpoint);

            return new LogRecord
            {
                Timestamp = ParseTimestamp(record, receiveTime),
                Origin = ExtractSource(record, endpoint),
                RawData = text,
                ParsedProperties = new Dictionary<string, string>()
                {
                    {"SourceModuleName", record.SourceModuleName},
                    {"SourceName", record.SourceName},
                    {"Severity", record.Severity},
                    {"Message", record.Message},
                },
            };
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
