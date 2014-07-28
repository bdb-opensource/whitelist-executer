using System.Web;
using System.Web.Mvc;
using WhitelistExecuter.Web.App_Start;

namespace WhitelistExecuter.Web
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new ErrorHandler());
        }
    }
}