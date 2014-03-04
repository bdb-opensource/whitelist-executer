using System;
using System.Collections.Generic;
using System.Configuration;
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
            var myChannelFactory = new ChannelFactory<IWhitelistExecuter>("WhitelistExecuter.Lib.IWhitelistExecuter");
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