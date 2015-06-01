using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Diagnostics;
using System.Threading;

namespace NxlogAzureForwarder
{
    internal class Uploader
    {
        public Options Options { get; set; }

        private LogParser parser_;
        private CloudTable table_;
        private volatile bool end_;

        public Uploader()
        {
            parser_ = new LogParser();
        }

        private void ConnectIfNeeded()
        {
            if (table_ != null) return;
            var storageAccount = CloudStorageAccount.Parse(Options.ConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            tableClient.DefaultRequestOptions.PayloadFormat = TablePayloadFormat.JsonNoMetadata;
            table_ = tableClient.GetTableReference(Options.TableName);
            table_.CreateIfNotExists();
        }

        private void Send(LogEntity entity)
        {
            for (int i = 0; i < 3 && !end_; ++i)
            {
                try
                {
                    ConnectIfNeeded();
                    table_.Execute(TableOperation.InsertOrReplace(entity));
                    return;
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.Message ?? e.GetType().Name);
                    table_ = null;
                    Thread.Sleep(1000);
                }
            }
        }

        public void Run()
        {
            while (!end_)
            {
                try
                {
                    string line;
                    while (!end_ && (line = Console.ReadLine()) != null && !end_)
                    {
                        var entity = parser_.Parse(DateTime.UtcNow, Options.Hostname, line);
                        Send(entity);
                    }
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.ToString());
                }
            }
        }

        public void Stop()
        {
            end_ = true;
        }
    }
}
