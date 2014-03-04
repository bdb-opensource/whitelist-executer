using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace WhitelistExecuter.WinService
{
    // Provide the ProjectInstaller class which allows 
    // the service to be installed by the Installutil.exe tool
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        public static Configuration config = ConfigurationManager.OpenExeConfiguration(Assembly.GetAssembly(typeof(ProjectInstaller)).Location);
        public static readonly string ServiceName = GetAppSettingsValue("ServiceName");
        public static readonly string ServiceDisplayName = GetAppSettingsValue("ServiceDisplayName");

        private ServiceProcessInstaller process;
        private ServiceInstaller service;

        public ProjectInstaller()
        {
            process = new ServiceProcessInstaller();
            process.Account = ServiceAccount.LocalSystem;
            service = new ServiceInstaller();
            service.ServiceName = ServiceName;
            service.DisplayName = ServiceDisplayName;
            Installers.Add(process);
            Installers.Add(service);
        }

        private static string GetAppSettingsValue(string key)
        {
        return config.AppSettings.Settings.Cast<KeyValueConfigurationElement>().Single(x => x.Key == key).Value;
        }
    }

}
