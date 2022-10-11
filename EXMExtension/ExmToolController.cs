using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using EXMExtension.Models;
using Sitecore;
using Sitecore.Mvc.Extensions;

namespace EXMExtension
{
    public class ExmToolController : Controller
    {

        private ExmToolGlobalModel model;
        public ExmToolController()
        {
            model = new ExmToolGlobalModel();
        }
        
        [HttpGet]
        public ActionResult Index()
        {

            return PartialView("~/Views/ExmTools/Home.cshtml",model);
        }

        [HttpGet]
        public ActionResult GenerateContacts()
        {
            return View("~/Views/ExmTools/GenerateContacts.cshtml", model);
        }

        /*
        [HttpPost]
        public ActionResult GeneratingData()
        {
            return View();
        }*/

        

    }
}
