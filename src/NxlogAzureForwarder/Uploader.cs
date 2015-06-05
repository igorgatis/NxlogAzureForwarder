using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Xml;

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

        public bool Upload(ITableEntity entity)
        {
            for (int i = 0; !_end && i < kNumberOfAttempts; ++i)
            {
                try
                {
                    ConnectIfNeeded();
                    _table.Execute(TableOperation.InsertOrReplace(entity));
                    return true;
                }
                catch (Exception e)
                {
                    ReporException(e);
                }
                _table = null;
                _endEvent.WaitOne(TimeSpan.FromSeconds(1));
            }
            return false;
        }

        public void Stop()
        {
            _end = true;
            _endEvent.Set();
        }

        private void ReporException(Exception exception)
        {
            var buffer = new StringBuilder();
            try
            {
                var se = exception as StorageException;
                if (se != null)
                {
                    se.RequestInformation.WriteXml(XmlTextWriter.Create(buffer));
                }
            }
            catch { }
            if (buffer.Length == 0)
            {
                buffer.Append(exception);
            }
            Trace.TraceError(buffer.ToString());
        }
    }
}
