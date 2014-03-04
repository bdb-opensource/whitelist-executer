using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WhitelistExecuter.Lib;

namespace WhitelistExecuter.Web.Models
{
    public class HomeModel
    {
        [Required]
        public Command Command { get; set; }

        [Required]
        [Display(Name = "Base path")]
        public string BaseDir { get; set; }
        public SelectListItem[] AvailableBaseDirs { get; set; }

        [Display(Name = "Relative path")]
        public string RelativePath { get; set; }
        public SelectListItem[] AvailableRelativePaths { get; set; }

        public string Error { get; set; }
        public string StandardOutput { get; set; }
        public string StandardError { get; set; }

        public string LastCommandPath { get; set; }

        public string Target { get; set; }

        public SelectListItem[] AvailableTargets { get; set; }
    }
}