using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace NxlogAzureForwarder
{
    internal class HttpServer
    {
        private class Statistics
        {
            public DateTime LastUploadAttemptTimestamp;
            public long UploadSuccessCount;
            public long UploadFailureCount;
        }
        private Statistics _statistics;

        private string _hostname;
        private LogParser _parser;
        private Uploader _uploader;
        private HttpListener _listener;

        public HttpServer(string hostname, LogParser parser, Uploader uploader)
        {
            _hostname = hostname;
            _statistics = new Statistics();
            _parser = parser;
            _uploader = uploader;
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:8514/");
            _listener.Prefixes.Add("http://127.0.0.1:8514/");
        }

        public void Stop()
        {
            _uploader.Stop();
            _listener.Stop();
            _listener.Close();
        }

        public void Start()
        {
            _listener.Start();
            ThreadPool.QueueUserWorkItem((o) => ListenToRequests());
        }

        private void ListenToRequests()
        {
            Trace.TraceInformation("Listening...");
            try
            {
                while (_listener.IsListening)
                {
                    ThreadPool.QueueUserWorkItem((c) =>
                    {
                        try
                        {
                            HandleRequest((HttpListenerContext)c);
                        }
                        catch (Exception e)
                        {
                            Trace.TraceError(e.ToString());
                        }
                    }, _listener.GetContext());
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }
            Trace.TraceInformation("Done listening.");
        }

        private void HandleRequest(HttpListenerContext context)
        {
            try
            {
                var path = (context.Request.Url.AbsolutePath ?? "").ToLower();
                switch (path)
                {
                    case "/ping":
                        Reply(context.Response, "pong");
                        return;
                    case "/upload":
                        var success = UploadRecords(context);
                        Reply(context.Response, statusCode: success ? 200 : 500);
                        return;
                    case "/stats":
                        ReplyJson(context.Response, _statistics);
                        return;
                }
                Reply(context.Response, "", statusCode: 404);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Reply(context.Response, string.Format("Error: {0}", e.Message), statusCode: 500);
            }
            finally
            {
                context.Response.OutputStream.Close();
            }
        }

        private void Reply(HttpListenerResponse response,
            string content = null, string contentType = null, int statusCode = 200)
        {
            response.StatusCode = statusCode;
            if (content != null)
            {
                response.ContentType = contentType ?? "text/plain";
                var writer = new StreamWriter(response.OutputStream, Encoding.UTF8);
                writer.Write(content);
                writer.Flush();
            }
        }

        private void ReplyJson<T>(HttpListenerResponse response, T obj, int statusCode = 200)
        {
            Reply(response, JsonConvert.SerializeObject(obj), "application/json", statusCode);
        }

        private bool UploadRecords(HttpListenerContext context)
        {
            _statistics.LastUploadAttemptTimestamp = DateTime.UtcNow;
            using (var reader = new StreamReader(context.Request.InputStream))
            {
                // Source should send one line at a time.
                // We handle multiple lines to be on the safe side.
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    bool success = false;
                    try
                    {
                        var record = _parser.Parse(DateTime.UtcNow, _hostname, line);
                        success = _uploader.Upload(record);
                    }
                    catch { }
                    if (!success)
                    {
                        Interlocked.Increment(ref _statistics.UploadFailureCount);
                        return false;
                    }
                    Interlocked.Increment(ref _statistics.UploadSuccessCount);
                }
            }
            return true;
        }
    }
}
