using System;
using System.Collections.Generic;
using Xunit;

namespace NxlogAzureForwarder.Tests
{
    public class PropertyRenderTests
    {
        private static readonly DateTime kTimestamp = new DateTime(2015, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc);

        [Fact]
        public void EventTimestamp()
        {
            var record = new LogRecord { EventTimestamp = kTimestamp };
            Assert.Equal("2015-01-02T03:04:05.6780000Z", PropertyRender.Render("${EventTimestamp:o}", record));
            Assert.Equal("2015-01-02", PropertyRender.Render("${EventTimestamp:yyyy-MM-dd}", record));
            Assert.Equal("03:04:05", PropertyRender.Render("${EventTimestamp:HH:mm:ss}", record));
            Assert.Equal("67800", PropertyRender.Render("${EventTimestamp:fffff}", record));
        }

        [Fact]
        public void Origin()
        {
            var record = new LogRecord { Origin = "foo_bar" };
            Assert.Equal("foo_bar", PropertyRender.Render("${Origin}", record));
        }

        [Fact]
        public void RawData()
        {
            var record = new LogRecord { RawData = "test 123" };
            Assert.Equal("test 123", PropertyRender.Render("${RawData}", record));
            Assert.Equal("-10291359", PropertyRender.Render("${RawData.GetHashCode()}", record));
            Assert.Equal("FF62F761", PropertyRender.Render("${RawData.GetHashCode():X8}", record));
        }

        [Fact]
        public void DefaultPartitionAndRowKey()
        {
            var record = new LogRecord
            {
                Origin = "foo_bar",
                EventTimestamp = kTimestamp,
                RawData = "test 123",
            };
            Assert.Equal("2015-01-02T03:00:00___foo_bar",
                PropertyRender.Render("${EventTimestamp:yyyy-MM-ddTHH:00:00}___${Origin}", record));
            Assert.Equal("2015-01-02T03:04:05.6780000Z___FF62F761",
                PropertyRender.Render("${EventTimestamp:o}___${RawData.GetHashCode():X8}", record));
        }

        private class Foo
        {
            public override string ToString()
            {
                return "Foo!";
            }
        }

        [Fact]
        public void ParsedData()
        {
            var record = new LogRecord
            {
                ParsedData = new Dictionary<string, object>()
                {
                    {"nullKey", null},
                    {"strKey", "strValue"},
                    {"intKey", 42},
                    {"floatKey", 0.5},
                    {"dateKey", kTimestamp},
                    {"objectKey", new object()},
                    {"fooKey", new Foo()},
                },
            };
            Assert.Equal("", PropertyRender.Render("${nullKey}", record));
            Assert.Equal("", PropertyRender.Render("${missingKey}", record));
            Assert.Equal("strValue", PropertyRender.Render("${strKey}", record));
            Assert.Equal("42", PropertyRender.Render("${intKey}", record));
            Assert.Equal("0.5", PropertyRender.Render("${floatKey}", record));
            Assert.Equal("01/02/2015 03:04:05", PropertyRender.Render("${dateKey}", record));
            Assert.Equal("2015-01-02T03:04:05.6780000Z", PropertyRender.Render("${dateKey:o}", record));
            Assert.Equal("System.Object", PropertyRender.Render("${objectKey}", record));
            Assert.Equal("Foo!", PropertyRender.Render("${fooKey}", record));
        }

        [Fact]
        public void Inverted()
        {
            var record = new LogRecord
            {
                Origin = "foo_bar",
                EventTimestamp = kTimestamp,
                RawData = "test 123",
            };
            Assert.Equal("7984-98-97G96:99:99___foo_bar",
                PropertyRender.Render("~{EventTimestamp:yyyy-MM-ddTHH:00:00}___${Origin}", record));
            Assert.Equal("7984-98-97G96:95:94.3219999A___FF62F761",
                PropertyRender.Render("~{EventTimestamp:o}___${RawData.GetHashCode():X8}", record));
        }
    }
}
