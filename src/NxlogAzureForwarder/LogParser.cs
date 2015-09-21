using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace NxlogAzureForwarder
{
    public class LogRecord
    {
        public DateTime EventTimestamp { get; set; }
        public string Origin { get; set; }
        public string RawData { get; set; }
        public Dictionary<string, object> ParsedData { get; set; }

        public object Resolve(string property)
        {
            switch (property)
            {
                case "Origin":
                    return Origin;
                case "EventTimestamp":
                    return EventTimestamp;
                case "RawData":
                    return RawData;
                case "RawData.GetHashCode()":
                    return (RawData ?? "").GetHashCode();
            }
            try
            {
                return ParsedData[property];
            }
            catch { }
            return null;
        }
    }

    internal class LogParser
    {
        public int MaxJsonSize { get; set; }

        public LogParser()
        {
            MaxJsonSize = (60 * 1024) / 2;
        }

        private readonly static DateTime kEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public LogRecord Parse(DateTime receiveTime, string endpoint, string rawData)
        {
            rawData = rawData ?? "";
            Dictionary<string, object> parsedData = null;
            try
            {
                parsedData = JsonConvert.DeserializeObject<Dictionary<string, object>>(rawData);
            }
            catch
            {
                // TODO(gatis): add counter.
            }
            if (parsedData == null)
            {
                parsedData = new Dictionary<string, object>()
                {
                    {"EventReceivedTime", receiveTime},
                    {"Hostname", endpoint},
                    {"Message", rawData},
                };
            }

            var time = ExtractTimestamp(parsedData, receiveTime);
            var origin = ExtractOrigin(parsedData, endpoint);
            var message = ExtractMessage(parsedData);
            return new LogRecord
            {
                EventTimestamp = time,
                Origin = origin,
                ParsedData = parsedData,
                RawData = ToJson(time, origin, message, parsedData),
            };
        }

        private void SetCoreFields(DateTime time, string origin, string message, Dictionary<string, object> dict)
        {
            var timeIso8601 = time.ToUniversalTime().ToString("o");
            dict["EventTimeIso8601"] = timeIso8601;
            dict["Message"] = message;
            using (var md5digest = MD5.Create())
            {
                var uniqueInfo = string.Join(timeIso8601, origin, message);
                byte[] md5sig = md5digest.ComputeHash(Encoding.UTF8.GetBytes(uniqueInfo));
                dict["EventUUID"] = BitConverter.ToString(md5sig).Replace("-", string.Empty).ToLower();
            }
        }

        private string ToJson(DateTime time, string origin, string message, Dictionary<string, object> dict)
        {
            SetCoreFields(time, origin, message, dict);
            var json = JsonConvert.SerializeObject(dict);

            // Truncation.
            if (json.Length > MaxJsonSize)
            {
                int delta = json.Length - MaxJsonSize;
                message = message.Substring(0, Math.Max(0, message.Length - delta));
                SetCoreFields(time, origin, message, dict);
                json = JsonConvert.SerializeObject(dict);
            }

            return json;
        }

        private DateTime ExtractTimestamp(Dictionary<string, object> record, DateTime defaultValue)
        {
            try
            {
                return Convert.ToDateTime(record["EventTimeIso8601"]).ToUniversalTime();
            }
            catch { }
            try
            {
                var microseconds = Convert.ToInt64(record["EventUnixTimeUs"]);
                return kEpoch + TimeSpan.FromMilliseconds(microseconds / 1000.0);
            }
            catch { }
            try
            {
                var milliseconds = Convert.ToInt64(record["EventUnixTimeMs"]);
                return kEpoch + TimeSpan.FromMilliseconds(milliseconds);
            }
            catch { }
            try
            {
                return Convert.ToDateTime(record["EventTime"]).ToUniversalTime();
            }
            catch { }
            try
            {
                return Convert.ToDateTime(record["EventReceivedTime"]).ToUniversalTime();
            }
            catch { }
            return defaultValue;
        }

        private string Get(Dictionary<string, object> record, string key)
        {
            object value = null;
            if (record.TryGetValue(key, out value) && value is string)
            {
                return (string)value;
            }
            return "";
        }

        private string ExtractOrigin(Dictionary<string, object> record, string defaultValue)
        {
            var source = new StringBuilder();
            AppendIfNotBlank(source, Get(record, "DeploymentId"));
            AppendIfNotBlank(source, Get(record, "RoleInstance"));

            if (source.Length == 0) AppendIfNotBlank(source, Get(record, "Hostname"));

            return source.Length > 0 ? source.ToString() : defaultValue;
        }

        private void AppendIfNotBlank(StringBuilder buffer, string part)
        {
            if (string.IsNullOrWhiteSpace(part)) return;
            if (buffer.Length > 0) buffer.Append("___");
            buffer.Append(part.Trim());
        }

        private string ExtractMessage(Dictionary<string, object> record)
        {
            try
            {
                return (string)record["Message"];
            }
            catch { }
            return string.Empty;
        }
    }
}
