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
            public bool Debug { get; set; }
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
            int expectation = 0;
            int actual = 0;
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
                    expectation += 1;
                    var entity = ConvertToEntity(record);
                    var result = _table.Execute(TableOperation.InsertOrReplace(entity));
                    var statusGroup = result.HttpStatusCode / 100;
                    if (statusGroup != 2 && statusGroup != 4)
                    {
                        if (_options.Debug) Trace.TraceError("HTTP status: " + result.HttpStatusCode);
                        return false;
                    }
                    actual += 1;
                }
            }
            catch (Exception e)
            {
                _table = null;
                if (_options.Debug) Trace.TraceError(e.ToString());
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
                    expectation += 2;
                    _queue.AddMessage(Serialize(record));
                    actual += 2;
                }
            }
            catch (Exception e)
            {
                _queue = null;
                if (_options.Debug) Trace.TraceError(e.ToString());
                return false;
            }

            if (_options.Debug && actual != expectation)
            {
                Trace.TraceError(string.Format("expectation={0} != {1}=actual", expectation, actual));
            }

            return expectation == actual;
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
