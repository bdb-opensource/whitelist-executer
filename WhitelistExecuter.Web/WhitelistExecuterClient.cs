using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Web;
using WhitelistExecuter.Lib;

namespace WhitelistExecuter.Web
{
    public class WhitelistExecuterClient : IDisposable
    {
        public IWhitelistExecuter API { get; protected set; }

        public WhitelistExecuterClient()
        {
            var myBinding = new BasicHttpBinding();
            var myEndpoint = new EndpointAddress("http://localhost/");
            var myChannelFactory = new ChannelFactory<IWhitelistExecuter>(myBinding, myEndpoint);
            this.API = myChannelFactory.CreateChannel();
        }


        #region IDisposable Members

        public void Dispose()
        {
            if (this.API != null)
            {
                ((ICommunicationObject)this.API).Close();
                ((ICommunicationObject)this.API).Abort();
                this.API = null;
            }
        }

        #endregion
    }
}