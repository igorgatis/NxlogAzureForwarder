﻿using Microsoft.WindowsAzure.Storage;
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
        private const string kTableNameKey = "AzureTableName";

        private HttpServer server_;

        public NxlogAzureForwarderService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            string connectionString = null;
            string tableName = null;
            try
            {
                connectionString = ConfigurationManager.AppSettings[kConnectionStringKey];
                tableName = ConfigurationManager.AppSettings[kTableNameKey];

                // Try parsing connection string.
                CloudStorageAccount.Parse(connectionString);

                server_ = new HttpServer(
                    Dns.GetHostName(),
                    new LogParser(),
                    new Uploader(connectionString, tableName));
                server_.Start();
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Trace.TraceError(string.Format("{0}: {1}{2}{3}: {4}...",
                    kTableNameKey, tableName, Environment.NewLine, kConnectionStringKey,
                    connectionString.Substring(0, Math.Min(60, connectionString.Length))));
                Stop();
            }
        }

        protected override void OnStop()
        {
            try
            {
                if (server_ != null) server_.Stop();
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }
        }
    }
}
