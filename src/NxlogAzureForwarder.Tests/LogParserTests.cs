using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Xunit;

namespace NxlogAzureForwarder.Tests
{
    public class LogParserTests
    {
        //private static readonly DateTime kTimestamp = new DateTime(2015, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc);

        private static readonly DateTime kTimeRecevied = new DateTime(2015, 6, 15, 12, 34, 56, DateTimeKind.Utc);

        [Fact]
        public void JsonTooLarge()
        {
            const int kSize = 256;
            var parser = new LogParser()
            {
                MaxJsonSize = kSize,
            };

            var json = "{\"Message\":\"" + new string('x', kSize) + "\"}";
            var record = parser.Parse(kTimeRecevied, "localhost", json);
            Assert.Equal(256, record.RawData.Length);

            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(record.RawData);
            var message = (string)dict["Message"];
            Assert.True(message.Length < kSize);
            Assert.Equal(new string('x', message.Length), message);
            Assert.Equal(message, (string)record.ParsedData["Message"]);
        }
    }
}
