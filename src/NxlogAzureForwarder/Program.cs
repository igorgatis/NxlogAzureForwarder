using System.ServiceProcess;

namespace NxlogAzureForwarder
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
            { 
                new NxlogAzureForwarderService() 
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
