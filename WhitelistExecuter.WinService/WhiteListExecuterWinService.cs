using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using WhitelistExecuter.Lib;

namespace WhitelistExecuter.WinService
{
    public class WhitelistExecuterWinService : ServiceBase
    {
        public ServiceHost serviceHost = null;

        public WhitelistExecuterWinService()
        {
            ServiceName = ProjectInstaller.ServiceName;
        }

        public static void Main()
        {
            ServiceBase.Run(new WhitelistExecuterWinService());
        }

        protected override void OnStart(string[] args)
        {
            if (serviceHost != null)
            {
                serviceHost.Close();
            }

            // Create a ServiceHost for the CalculatorService type and 
            // provide the base address.
            serviceHost = new ServiceHost(typeof(WhitelistExecuter.Lib.WhitelistExecuter));

            // Open the ServiceHostBase to create listeners and start 
            // listening for messages.
            serviceHost.Open();
        }

        protected override void OnStop()
        {
            if (serviceHost != null)
            {
                serviceHost.Close();
                serviceHost = null;
            }
        }
    }
}
