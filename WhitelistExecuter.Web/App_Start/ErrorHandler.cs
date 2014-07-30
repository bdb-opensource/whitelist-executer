using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using log4net;

namespace WhitelistExecuter.Web.App_Start
{
    public class ErrorHandler : HandleErrorAttribute
    {
        protected static readonly ILog _logger = log4net.LogManager.GetLogger("ErrorHandler");

        public override void OnException(ExceptionContext filterContext)
        {
            _logger.Error("ErrorHandler handled error", filterContext.Exception);
            base.OnException(filterContext);
        }
    }
}
