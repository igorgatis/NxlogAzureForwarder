using CommandLine;
using CommandLine.Text;
using System;
using System.Diagnostics;
using System.Net;

namespace NxlogAzureForwarder
{
    internal class Options
    {
        [Option("connection_string", DefaultValue = "", HelpText =
            @"Connection string. E.g. " +
            "'DefaultEndpointsProtocol=https;AccountName=accountname;AccountKey=accountkey'.")]
        public string ConnectionString { get; set; }

        [Option("table_name", Required = true, HelpText = "Table name.")]
        public string TableName { get; set; }

        [Option("hostname", Required = false, HelpText = "Hostname.")]
        public string Hostname { get; set; }

        [Option("stop_nxlog", Required = false, HelpText = "Stops NXLog in case of abnormal exit.")]
        public bool StopNxlog { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }

    class Program
    {
        private static void StopNxlog()
        {
            var startInfo = new ProcessStartInfo()
            {
                FileName = "cmd.exe",
                Arguments = "net stop nxlog",
                CreateNoWindow = true,
                UseShellExecute = false,
            };
            using (var proc = new Process { StartInfo = startInfo })
            {
                proc.Start();
                proc.WaitForExit();
            }
        }

        static int Main(string[] args)
        {
            Options options = null;
            try
            {
                options = new Options();
                var parser = new CommandLine.Parser(with => with.HelpWriter = Console.Error);
                parser.ParseArgumentsStrict(args, options, () =>
                {
                    Trace.TraceError("Failed to parse command line arguments.");
                    Environment.Exit(-2);
                });

                if (string.IsNullOrWhiteSpace(options.Hostname))
                {
                    try
                    {
                        options.Hostname = Dns.GetHostName();
                    }
                    catch { }
                }

                var program = new Uploader { Options = options };
                Console.CancelKeyPress += delegate
                {
                    program.Stop();
                };
                program.Run();
                return 0;
            }
            catch (Exception e)
            {
                if (options != null && options.StopNxlog)
                {
                    StopNxlog();
                }
                Trace.TraceError(e.ToString());
                return -1;
            }
        }
    }
}
