using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebMatrix.WebData;
using WhitelistExecuter.Lib;
using WhitelistExecuter.Web.Filters;
using WhitelistExecuter.Web.Models;

namespace WhitelistExecuter.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View(new HomeModel()
            {
                AvailableRelativePaths = GetAvailableRelativePaths()
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult ExecuteCommand(HomeModel model)
        {
            model.Error = null;
            if (null == model.AvailableRelativePaths || (false == model.AvailableRelativePaths.Any()))
            {
                model.AvailableRelativePaths = GetAvailableRelativePaths();
            }

            using (var client = new WhitelistExecuterClient())
            {
                ExecutionResult result;
                try
                {
                    result = client.API.ExecuteCommand(model.Command, model.RelativePath);
                }
                catch (Exception e)
                {
                    model.Error = (e.InnerException ?? e).Message;
                    return View("Index", model);
                }
                //.....ViewBag.ViewBag.mo
                model.StandardOutput += result.StandardOutput.Trim();
                model.StandardError += result.StandardError.Trim();
            }
            return View("Index", model);
        }


        private static SelectListItem[] GetAvailableRelativePaths()
        {
            return GetPaths().Select(x => new SelectListItem()
            {
                Text = x,
                Value = x,
                Selected = false
            }).ToArray();
        }

        private static string[] GetPaths()
        {
            string[] paths;
            if (WebSecurity.IsAuthenticated)
            {
                using (var client = new WhitelistExecuterClient())
                {
                    paths = client.API.GetPaths();
                }
            }
            else
            {
                paths = new string[0];
            }
            return paths;
        }

    }
}
