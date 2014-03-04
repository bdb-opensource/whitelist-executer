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
        [Display(Name = "Relative path")]
        public string RelativePath { get; set; }

        public SelectListItem[] AvailableRelativePaths { get; set; }

        public string Error { get; set; }
        public string StandardOutput { get; set; }
        public string StandardError { get; set; }
    }
}