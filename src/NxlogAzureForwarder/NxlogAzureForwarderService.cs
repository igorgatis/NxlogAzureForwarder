using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.ServiceProcess;

namespace NxlogAzureForwarder
{
    public partial class NxlogAzureForwarderService : ServiceBase
    {
        private const string kConnectionString = "ConnectionString";
        private const string kQueueName = "QueueName";
        private const string kTableName = "TableName";
        private const string kPartitionKey = "PartitionKey";
        private const string kRowKey = "RowKey";
        private const string kAditionalColumns = "AditionalColumns";

        private HttpServer _server;

        public NxlogAzureForwarderService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                var account = CloudStorageAccount.Parse(appSettings[kConnectionString]);
                var options = new Uploader.Options
                {
                    QueueName = appSettings[kQueueName],
                    TableName = appSettings[kTableName],
                    PartitionKey = appSettings[kPartitionKey],
                    RowKey = appSettings[kRowKey],
                    AditionalColumns = new HashSet<string>(appSettings[kAditionalColumns].Split(',')),
                };

                Trace.TraceInformation(JsonConvert.SerializeObject(options));
                if (string.IsNullOrEmpty(options.TableName) &&
                    string.IsNullOrEmpty(options.QueueName))
                {
                    throw new Exception("No output specified.");
                }

                _server = new HttpServer(Dns.GetHostName(), new Uploader(account, options));
                _server.Start();
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Stop();
            }
        }

        protected override void OnStop()
        {
            try
            {
                if (_server != null) _server.Stop();
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }
        }
    }
}
