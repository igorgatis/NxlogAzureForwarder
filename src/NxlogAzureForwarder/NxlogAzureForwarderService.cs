using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.ServiceProcess;

namespace NxlogAzureForwarder
{
    public partial class NxlogAzureForwarderService : ServiceBase
    {
        private const string kConnectionStringKey = "AzureStorageConnectionString";
        private const string kAzureTableNameKey = "AzureTableName";
        private const string kIncludeExtraColumnsKey = "IncludeExtraColumns";
        private const string kAzureQueueNameKey = "AzureQueueName";
        private const string kFatQueueMessageKey = "FatQueueMessage";

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
                var account = CloudStorageAccount.Parse(appSettings[kConnectionStringKey]);
                var options = new Uploader.Options
                {
                    TableName = appSettings[kAzureTableNameKey],
                    IncludeExtraColumns = bool.Parse(appSettings[kIncludeExtraColumnsKey]),
                    QueueName = appSettings[kAzureQueueNameKey],
                    FatQueueMessage = bool.Parse(appSettings[kFatQueueMessageKey]),
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
