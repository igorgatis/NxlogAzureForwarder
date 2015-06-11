using NxlogAzureForwarder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(PropertyRender.Render("${Timestamp:o} ${RoleName}${RoleInstance}",
                new LogRecord
                {
                    Origin = "esfera5-01",
                    RawData = "dsf sdaf sdf as",
                    Timestamp = DateTime.UtcNow,
                    ParsedData = new Dictionary<string, object>
                    {
                        {"Source", "sourcename"},
                        {"Timestamp", new DateTime(2006,02,06, 12,34,56).ToString("o")},
                        {"Message", "sourcename"},
                        {"DeploymentId", Guid.NewGuid()},
                        //{"RoleName", "E5.Gateway"},
                        {"RoleInstance", "E5.Gateway_IN_0"},
                    },
                }));
        }
    }
}
