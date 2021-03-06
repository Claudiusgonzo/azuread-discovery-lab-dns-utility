﻿using AzureADLabDNSControl.Infra;
using Lab.Common;
using System.Web.Mvc;
using System.Web.Security;

namespace CSAHub.Areas.Admin.Controllers
{
    [AdminAuthorize(Roles = CustomRoles.LabAdmin)]
    public class ErrorLogController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.SiteName = "Azure AD Lab";
            return View();
        }
    }
}
