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
			public string[] TableNames { get; set; }
			public string[] QueueNames { get; set; }

			public string PartitionKey { get; set; }
			public string RowKey { get; set; }
			public HashSet<string> AditionalColumns { get; set; }
			public bool Debug { get; set; }
		}

		private TableUploader _tables;
		private QueueUploader _queues;

		public Uploader(CloudStorageAccount account, Options options)
		{
			_tables = new TableUploader(account, options.TableNames,
				options.PartitionKey, options.RowKey, options.AditionalColumns, options.Debug);
			_queues = new QueueUploader(account, options.QueueNames, options.Debug);
		}

		internal bool Upload(LogRecord record)
		{
			var tableOk = _tables.Upload(record);
			var queueOk = _queues.Upload(record);
			return tableOk && queueOk;
		}

		private abstract class RoundRobinUploader<TCollection>
			where TCollection : class
		{
			protected CloudStorageAccount _account;
			protected string[] _names;
			protected bool _debug;

			private TCollection[] _list;
			private int _index;

			public bool Enabled { get { return _list.Length > 0; } }

			public RoundRobinUploader(CloudStorageAccount account, string[] names, bool debug)
			{
				_account = account;
				_names = names;
				_list = new TCollection[_names.Length];
				_debug = debug;
			}

			public abstract TCollection SetupCollection(string name);

			public abstract bool Upload(TCollection col, LogRecord record);

			public bool Upload(LogRecord record)
			{
				if (_list.Length == 0) return true;

				for (int i = 0; i < _list.Length; ++i)
				{
					_index = (_index + 1) % _list.Length;
					try
					{
						if (_list[_index] == null)
						{
							_list[_index] = SetupCollection(_names[_index]);
						}
						if (_list[_index] != null)
						{
							if (Upload(_list[_index], record))
							{
								return true;
							}
							_list[_index] = null;
						}
					}
					catch (Exception e)
					{
						if (_debug) Trace.TraceError(e.ToString());
						_list[_index] = null;
					}
				}
				return false;
			}
		}

		private class TableUploader : RoundRobinUploader<CloudTable>
		{
			private string _partitionKey;
			private string _rowKey;
			private HashSet<string> _aditionalColumns;

			public TableUploader(CloudStorageAccount account, string[] names,
				string partitionKey, string rowKey, HashSet<string> aditionalColumns,
				bool debug)
				: base(account, names, debug)
			{
				_partitionKey = partitionKey;
				_rowKey = rowKey;
				_aditionalColumns = aditionalColumns;
			}

			public override CloudTable SetupCollection(string name)
			{
				var table = _account.CreateCloudTableClient()
					.GetTableReference(name);
				table.CreateIfNotExists();
				return table;
			}

			public override bool Upload(CloudTable table, LogRecord record)
			{
				var entity = ConvertToEntity(record);
				var result = table.Execute(TableOperation.InsertOrReplace(entity));
				var statusGroup = result.HttpStatusCode / 100;
				if (statusGroup != 2 && statusGroup != 4)
				{
					if (_debug) Trace.TraceError("HTTP status: " + result.HttpStatusCode);
					return false;
				}
				return true;
			}

			private DynamicTableEntity ConvertToEntity(LogRecord record)
			{
				var entity = new DynamicTableEntity
				{
					PartitionKey = PropertyRender.Render(_partitionKey, record),
					RowKey = PropertyRender.Render(_rowKey, record),
				};
				var propertiesAdded = new HashSet<string>(entity.Properties.Keys);
				if (record.ParsedData != null)
				{
					foreach (var key in _aditionalColumns)
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

		private class QueueUploader : RoundRobinUploader<CloudQueue>
		{
			public QueueUploader(CloudStorageAccount account, string[] queueNames, bool debug)
				: base(account, queueNames, debug) { }

			public override CloudQueue SetupCollection(string name)
			{
				var queue = _account.CreateCloudQueueClient()
					.GetQueueReference(name);
				queue.CreateIfNotExists();
				return queue;
			}

			public override bool Upload(CloudQueue queue, LogRecord record)
			{
				var lean = new LogRecord
				{
					Origin = record.Origin,
					EventTimestamp = record.EventTimestamp,
					RawData = record.RawData ?? "",
				};
				queue.AddMessage(new CloudQueueMessage(JsonConvert.SerializeObject(lean)));
				return true;
			}
		}
	}
}
