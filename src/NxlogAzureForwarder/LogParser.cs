using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Text;

namespace NxlogAzureForwarder
{
    internal class LogEntity : TableEntity
    {
        public string Data { get; set; }
    }

    internal class LogParser
    {
        private readonly static DateTime kEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private const int kTickResolution = 100000000;

        private class JsonLine
        {
            public string EventUnixTimeMs { get; set; }
            public string EventReceivedTime { get; set; }

            public string DeploymentId { get; set; }
            public string RoleName { get; set; }
            public string RoleInstance { get; set; }

            public string Hostname { get; set; }
        }

        public static LogEntity NewEntity(DateTime time, string source, string data)
        {
            var mostSigTime = string.Format("{0:D19}", kTickResolution * (time.Ticks / kTickResolution));
            var leastSigTime = string.Format("{0:D19}", time.Ticks % kTickResolution);

            var hash = data.GetHashCode();
            var row = string.Join("___", source, leastSigTime, hash.ToString("x8"));

            return new LogEntity
            {
                PartitionKey = mostSigTime,
                RowKey = row,
                Data = data,
            };
        }

        public LogEntity Parse(DateTime receiveTime, string endpoint, string jsonText)
        {
            jsonText = jsonText ?? "";
            JsonLine record;
            try
            {
                record = JsonConvert.DeserializeObject<JsonLine>(jsonText);
            }
            catch
            {
                record = new JsonLine();
            }
            var time = ParseTimestamp(record, receiveTime);
            var source = ExtractSource(record, endpoint);
            return NewEntity(time, source, jsonText);
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
