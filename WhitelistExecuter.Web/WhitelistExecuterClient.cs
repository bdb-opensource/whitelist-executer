using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Configuration;
using System.Web;
using WhitelistExecuter.Lib;

namespace WhitelistExecuter.Web
{
    public class WhitelistExecuterClient : IDisposable
    {
        public Dictionary<string, IWhitelistExecuter> APIs { get; protected set; }

        public WhitelistExecuterClient()
        {
            this.APIs = new Dictionary<string, IWhitelistExecuter>();
            var names = GetEndpointNames();
            foreach (var name in names)
            {
                var myChannelFactory = new ChannelFactory<IWhitelistExecuter>(name);
                this.APIs[name] = myChannelFactory.CreateChannel();
            }
        }

        public static IEnumerable<string> GetEndpointNames()
        {
            ClientSection clientSection = ConfigurationManager.GetSection("system.serviceModel/client") as ClientSection;
            ChannelEndpointElementCollection endpointCollection =
                clientSection.ElementInformation.Properties[string.Empty].Value as ChannelEndpointElementCollection;
            return endpointCollection.Cast<ChannelEndpointElement>().Select(x => x.Name);
        }


        #region IDisposable Members

        public void Dispose()
        {
            if (this.APIs != null)
            {
                foreach (var api in this.APIs.Values)
                {
                    ((ICommunicationObject)api).Close();
                    ((ICommunicationObject)api).Abort();
                }
                this.APIs = null;
            }
        }

        #endregion
    }
}