using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Diagnostics;
using System.Threading;

namespace NxlogAzureForwarder
{
    internal class Uploader
    {
        private const int kNumberOfAttempts = 3;

        private string _connectionString;
        private string _tableName;

        private volatile bool _end;
        private ManualResetEvent _endEvent;
        private CloudTable _table;

        public Uploader(string connectionString, string tableName)
        {
            _endEvent = new ManualResetEvent(false);
            _connectionString = connectionString;
            _tableName = tableName;
        }

        private void ConnectIfNeeded()
        {
            if (_table != null) return;
            var storageAccount = CloudStorageAccount.Parse(_connectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            tableClient.DefaultRequestOptions.PayloadFormat = TablePayloadFormat.JsonNoMetadata;
            _table = tableClient.GetTableReference(_tableName);
            _table.CreateIfNotExists();
        }

        public bool Upload(LogEntity entity)
        {
            var insert = TableOperation.InsertOrReplace(entity);
            for (int i = 0; !_end && i < kNumberOfAttempts; ++i)
            {
                try
                {
                    ConnectIfNeeded();
                    _table.Execute(insert);
                    return true;
                }
                catch (Exception e)
                {
                    _table = null;
                    Trace.TraceError(e.ToString());
                    _endEvent.WaitOne(TimeSpan.FromSeconds(1));
                }
            }
            return false;
        }

        public void Stop()
        {
            _end = true;
            _endEvent.Set();
        }
    }
}
