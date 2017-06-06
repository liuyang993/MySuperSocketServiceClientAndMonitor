using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
namespace MyRouteService
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
                new RouteService()
            };
            ServiceBase.Run(ServicesToRun);

            /*   for debug purpose
            #if(!DEBUG)
                        ServiceBase[] ServicesToRun;
                        ServicesToRun = new ServiceBase[]
                        {
                            new RouteService()
                        };
                        ServiceBase.Run(ServicesToRun);

            #else // If you are currently in debug mode
                        string[] args = { "a", "b" };
                        RouteService service = new RouteService(); // create your service's instance
                        service.Start(args); // start this service
                        Thread.Sleep(Timeout.Infinite);

            #endif

                */
        }
    }
}
