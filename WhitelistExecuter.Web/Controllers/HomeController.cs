using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using log4net;
using WebMatrix.WebData;
using WhitelistExecuter.Lib;
using WhitelistExecuter.Web.Filters;
using WhitelistExecuter.Web.Models;

namespace WhitelistExecuter.Web.Controllers
{
    public class HomeController : Controller
    {
        protected static readonly ILog _logger = log4net.LogManager.GetLogger("HomeController");

        public ActionResult Index()
        {

            var targets = WhitelistExecuterClient.GetEndpointNames();
            var targetName = targets.First();
            return SetTarget(new HomeModel() { Target = targetName });
        }

        [Authorize]
        [HttpPost]
        public ActionResult SetTarget(HomeModel model)
        {
            return View("Index", GetModelForTarget(model.Target));
        }

        [Authorize]
        [HttpPost]
        public ActionResult SetBaseDir(HomeModel model)
        {
            var updatedModel = GetModelForTarget(model.Target);
            if (false == updatedModel.AvailableBaseDirs.Any(x => x.Value.Equals(model.BaseDir, StringComparison.InvariantCultureIgnoreCase))) 
            {
                throw new Exception("Base dir does not belong to current target");
            }
            updatedModel.BaseDir = model.BaseDir;
            updatedModel.AvailableRelativePaths = GetAvailableRelativePaths(updatedModel.Target, updatedModel.BaseDir);
            return View("Index", updatedModel);
        }

        private static HomeModel GetModelForTarget(string targetName)
        {
            var allPaths = GetPaths(targetName);
            var defaultBaseDir = (false == allPaths.Any())
                               ? null
                               : allPaths.First().Key;
            var model = new HomeModel()
            {
                Target = targetName,
                BaseDir = defaultBaseDir,
            };
            UpdateAvailableOptions(model);
            return model;
        }

        private static void UpdateAvailableOptions(HomeModel model)
        {
            model.AvailableTargets = ToSelectList(WhitelistExecuterClient.GetEndpointNames());
            model.AvailableBaseDirs = ToSelectList(GetPaths(model.Target).Select(x => x.Key));
            model.AvailableRelativePaths = GetAvailableRelativePaths(model.Target, model.BaseDir);
        }

        [Authorize]
        [HttpPost]
        public ActionResult ExecuteCommand(HomeModel model)
        {
            UpdateAvailableOptions(model);

            if (String.IsNullOrWhiteSpace(model.RelativePath))
            {
                model.RelativePath = String.Empty;
            }

            using (var client = new WhitelistExecuterClient())
            {
                ExecutionResult result;
                try
                {
                    _logger.InfoFormat("Executing command: {0} in {1}:{2}/{3}. User: {4}", model.Command, model.Target, model.BaseDir, model.RelativePath, WebSecurity.CurrentUserName);
                    result = client.APIs[model.Target].ExecuteCommand(model.BaseDir, model.Command, model.RelativePath);
                }
                catch (Exception e)
                {
                    model.Error = (e.InnerException ?? e).Message;
                    return View("Index", model);
                }
                //.....ViewBag.ViewBag.mo
                model.LastCommandPath = Path.Combine(model.BaseDir, model.RelativePath);
                model.StandardOutput += result.StandardOutput.Trim();
                model.StandardError += result.StandardError.Trim();
            }
            return View("Index", model);
        }


        private static SelectListItem[] GetAvailableRelativePaths(string target, string baseDir)
        {
            var paths = GetPaths(target);
            if (false == paths.Any())
            {
                return null;
            }
            var strs = paths.Single(x => x.Key.Equals(baseDir, StringComparison.InvariantCultureIgnoreCase))
                             .Value;
            return ToSelectList(strs);
        }

        private static SelectListItem[] ToSelectList(IEnumerable<string> strs)
        {
            return strs.Select(x => new SelectListItem()
            {
                Text = x,
                Value = x,
                Selected = false
            }).ToArray();
        }

        private static List<KeyValuePair<string, string[]>> GetPaths(string target)
        {
            List<KeyValuePair<string, string[]>> paths;
            if (WebSecurity.IsAuthenticated)
            {
                using (var client = new WhitelistExecuterClient())
                {
                    paths = client.APIs[target].GetPaths();
                }
            }
            else
            {
                paths = new List<KeyValuePair<string, string[]>>();
            }
            return paths;
        }

    }
}
