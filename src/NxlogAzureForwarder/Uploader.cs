using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Diagnostics;

namespace NxlogAzureForwarder
{
    internal class Uploader
    {
        private const int kMaxMessageLength = 60 * 1024;
        private const int kTickResolution = 100000000;

        public class Options
        {
            public string TableName { get; set; }
            public bool IncludeExtraColumns { get; set; }

            public string QueueName { get; set; }
            public bool FatQueueMessage { get; set; }
        }

        private class LogMessage
        {
            public string PartitionKey { get; set; }
            public string RowKey { get; set; }
            public DateTime Timestamp { get; set; }
            public string Origin { get; set; }
            public string RawData { get; set; }
        }

        private CloudStorageAccount _account;
        private Options _options;
        private CloudTable _table;
        private CloudQueue _queue;

        public Uploader(CloudStorageAccount account, Options options)
        {
            _account = account;
            _options = options;
        }

        private void ConnectIfNeeded()
        {
            if (_table == null && !string.IsNullOrEmpty(_options.TableName))
            {
                _table = _account.CreateCloudTableClient()
                    .GetTableReference(_options.TableName);
                _table.CreateIfNotExists();
            }
            if (_queue == null && !string.IsNullOrEmpty(_options.QueueName))
            {
                _queue = _account.CreateCloudQueueClient()
                    .GetQueueReference(_options.QueueName);
                _queue.CreateIfNotExists();
            }
        }

        private CloudQueueMessage Serialize(LogMessage message)
        {
            var text = JsonConvert.SerializeObject(message);
            if (text.Length > kMaxMessageLength && message.RawData != null)
            {
                int delta = text.Length - kMaxMessageLength;
                if (message.RawData.Length > delta)
                {
                    int length = message.RawData.Length - delta;
                    message.RawData = message.RawData.Substring(0, length);
                }
            }
            return new CloudQueueMessage(text);
        }

        private LogMessage ConvertToMessage(ITableEntity entity, LogRecord record)
        {
            var message = new LogMessage
            {
                PartitionKey = entity.PartitionKey,
                RowKey = entity.RowKey,
            };
            if (_options.FatQueueMessage)
            {
                message.Origin = record.Origin;
                message.Timestamp = record.Timestamp;
                message.RawData = record.RawData;
            }
            return message;
        }

        public bool Upload(LogRecord record)
        {
            try
            {
                var entity = ConvertToEntity(record);
                ConnectIfNeeded();
                if (_table != null)
                {
                    _table.Execute(TableOperation.InsertOrReplace(entity));
                }
                if (_queue != null)
                {
                    var message = ConvertToMessage(entity, record);
                    _queue.AddMessage(Serialize(message));
                }
                return true;
            }
            catch (Exception e)
            {
                _table = null;
                _queue = null;
                Trace.TraceError(e.ToString());
                return false;
            }
        }

        private DynamicTableEntity ConvertToEntity(LogRecord record)
        {
            var ticks = record.Timestamp.Ticks;
            var mostSigTime = string.Format("{0:D19}", kTickResolution * (ticks / kTickResolution));
            var leastSigTime = string.Format("{0:D19}", ticks % kTickResolution);

            var hash = record.RawData.GetHashCode();
            var row = string.Join("___", record.Origin, leastSigTime, hash.ToString("x8"));

            var entity = new DynamicTableEntity
            {
                PartitionKey = mostSigTime,
                RowKey = row,
            };
            if (_options.IncludeExtraColumns && record.ParsedProperties != null)
            {
                foreach (var pair in record.ParsedProperties)
                {
                    if (!string.IsNullOrEmpty(pair.Value))
                    {
                        entity[pair.Key] = new EntityProperty(pair.Value);
                    }
                }
            }
            entity["RawData"] = new EntityProperty(record.RawData);
            return entity;
        }
    }
}
