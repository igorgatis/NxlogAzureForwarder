using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NxlogAzureForwarder
{
    internal class Uploader
    {
        private const int kMaxMessageLength = 60 * 1024;

        public class Options
        {
            public string QueueName { get; set; }
            public string TableName { get; set; }
            public string PartitionKey { get; set; }
            public string RowKey { get; set; }
            public HashSet<string> AditionalColumns { get; set; }
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

        private CloudQueueMessage Serialize(LogRecord message)
        {
            var lean = new LogRecord
            {
                Origin = message.Origin,
                EventTimestamp = message.EventTimestamp,
                RawData = message.RawData,
            };
            var text = JsonConvert.SerializeObject(lean);
            if (text.Length > kMaxMessageLength && lean.RawData != null)
            {
                int delta = text.Length - kMaxMessageLength;
                if (lean.RawData.Length > delta)
                {
                    int length = lean.RawData.Length - delta;
                    lean.RawData = lean.RawData.Substring(0, length);
                }
            }
            // Message can still be bigger. But we can't safely strip
            // anything else at this point.
            return new CloudQueueMessage(text);
        }

        public bool Upload(LogRecord record)
        {
            try
            {
                if (_queue != null)
                {
                    _queue.AddMessage(Serialize(record));
                }

                var entity = ConvertToEntity(record);
                ConnectIfNeeded();
                if (_table != null)
                {
                    _table.Execute(TableOperation.InsertOrReplace(entity));
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
            var entity = new DynamicTableEntity
            {
                PartitionKey = PropertyRender.Render(_options.PartitionKey, record),
                RowKey = PropertyRender.Render(_options.RowKey, record),
            };
            if (record.ParsedData != null)
            {
                foreach (var pair in record.ParsedData)
                {
                    if (_options.AditionalColumns.Contains(pair.Key) && pair.Value != null)
                    {
                        entity[pair.Key] = EntityProperty.CreateEntityPropertyFromObject(pair.Value);
                    }
                }
            }
            entity["EventTimestamp"] = new EntityProperty(record.EventTimestamp);
            entity["Origin"] = new EntityProperty(record.Origin);
            entity["RawData"] = new EntityProperty(record.RawData);
            return entity;
        }
    }
}
