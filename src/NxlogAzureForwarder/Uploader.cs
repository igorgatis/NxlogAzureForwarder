using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NxlogAzureForwarder
{
    internal class Uploader
    {
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

        public bool Upload(LogRecord record)
        {
            try
            {
                if (_table == null && !string.IsNullOrEmpty(_options.TableName))
                {
                    _table = _account.CreateCloudTableClient()
                        .GetTableReference(_options.TableName);
                    _table.CreateIfNotExists();
                }
                if (_table != null)
                {
                    var entity = ConvertToEntity(record);
                    _table.Execute(TableOperation.InsertOrReplace(entity));
                }
            }
            catch (Exception e)
            {
                _table = null;
                Trace.TraceError(e.ToString());
                return false;
            }

            try
            {
                if (_queue == null && !string.IsNullOrEmpty(_options.QueueName))
                {
                    _queue = _account.CreateCloudQueueClient()
                        .GetQueueReference(_options.QueueName);
                    _queue.CreateIfNotExists();
                }
                if (_queue != null)
                {
                    _queue.AddMessage(Serialize(record));
                }
            }
            catch (Exception e)
            {
                _queue = null;
                Trace.TraceError(e.ToString());
                return false;
            }

            return true;
        }

        private CloudQueueMessage Serialize(LogRecord message)
        {
            var lean = new LogRecord
            {
                Origin = message.Origin,
                EventTimestamp = message.EventTimestamp,
                RawData = message.RawData ?? "",
            };
            return new CloudQueueMessage(JsonConvert.SerializeObject(lean));
        }

        private DynamicTableEntity ConvertToEntity(LogRecord record)
        {
            var entity = new DynamicTableEntity
            {
                PartitionKey = PropertyRender.Render(_options.PartitionKey, record),
                RowKey = PropertyRender.Render(_options.RowKey, record),
            };
            var propertiesAdded = new HashSet<string>(entity.Properties.Keys);
            if (record.ParsedData != null)
            {
                foreach (var key in _options.AditionalColumns)
                {
                    object value = record.Resolve(key);
                    if (value != null && propertiesAdded.Add(key))
                    {
                        entity[key] = EntityProperty.CreateEntityPropertyFromObject(value);
                    }
                }
            }
            if (propertiesAdded.Count == 0)
            {
                entity["RawData"] = new EntityProperty(record.RawData);
            }
            return entity;
        }
    }
}
