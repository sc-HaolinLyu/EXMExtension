using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Http;
using EXMExtension.Models;
using EXMExtension.Tools;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.localhost;
using Sitecore.Mvc.Extensions;
using Sitecore.Shell.Framework.Commands.TemplateBuilder;
using HttpGetAttribute = System.Web.Http.HttpGetAttribute;

namespace EXMExtension
{
    public class ExmToolController : Controller
    {

        private ExmToolGlobalModel model;
        public ExmToolController()
        {
            model = new ExmToolGlobalModel();
            this.ViewData["ExmTools"] = ExmToolGlobalModel.ToolMapping;
        }
        
        [System.Web.Http.HttpGet]
        public ActionResult Index()
        {

            return PartialView("~/Views/ExmTools/Home.cshtml",model);
        }


        /// <summary>
        /// Workflow.
        /// 1. The page is rendered upon initial request
        /// There are two status
        /// a. If there is no active task. The form should be rendered (possibly with errors in the model)
        /// b. If there is active task, a progress bar should be rendered
        /// </summary>
        /// <returns></returns>
        [System.Web.Mvc.AcceptVerbs(HttpVerbs.Get)]
        public ActionResult GenerateContacts(string toolKey = "GenerateContacts")
        {
            //Initialze task model for the first request
            if (!ExmToolGlobalModel.ActiveTasks.ContainsKey(toolKey))
            {
                ExmToolGlobalModel.ActiveTasks.Add(toolKey, new GenerateContactModel());
            }
            var contactModel = ExmToolGlobalModel.ActiveTasks[toolKey] as GenerateContactModel;
            return View("~/Views/ExmTools/GenerateContacts.cshtml", contactModel);
        }


        [System.Web.Mvc.AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ModifyContacts()
        {
            var contactOperation = Request.Form["contactOperation"];
            if (contactOperation == "0")
            {
                return GenerateContacts();
            }
            if(contactOperation == "1")
            {

                return RemoveContact();
            }

            return GenerateContacts("GenerateContacts");
        }

        private ActionResult RemoveContact()
        {
            string toolKey = "GenerateContacts";
            var contactModel = ExmToolGlobalModel.ActiveTasks[toolKey] as GenerateContactModel;
            ContactAndListMethods.SetContactInfo(ContactOperations.RemoveContact, contactModel);
            ContactAndListMethods.RemoveContacts(toolKey, 50);
            ContactAndListMethods.RemoveLists(toolKey);
            return GenerateContacts("GenerateContacts");
        }
        /// <summary>
        /// The post request takes in the form model.
        /// 1. Do the input validation. If the params are not correct, return the error in the model
        /// 2. Create the task and store it in the model
        /// 3. Based on the task result. Push the error in the error model or just show success information and show the form again
        /// </summary>
        /// <returns></returns>

        private ActionResult GenerateContacts()
        {
            string toolKey = "GenerateContacts";
            var contactNumInput = Request.Form["contactNumber"];
            var listNumInput = Request.Form["listNumber"];
            var contactModel = ExmToolGlobalModel.ActiveTasks[toolKey] as GenerateContactModel;
            contactModel.errorList.Clear();//clear old errors on each submission
            if (!int.TryParse(contactNumInput, out int contactNum))
            {
                contactModel.errorList.Add("The contact number can't be parsed");
            }
            else if(contactNum<=0||contactNum>10000)
            {
                contactModel.errorList.Add("The contact number should between 1-10000");
            }
            if (!int.TryParse(listNumInput, out int listNum))
            {
                contactModel.errorList.Add("The list number can't be parsed");
            }
            else if (listNum <= 0 || listNum > 10)
            {
                contactModel.errorList.Add("The list number should between 1-10");
            }
            if (contactModel.isActive)
            {
                contactModel.errorList.Add("There are active task currently");
            }

            if (contactModel.errorList.Count > 0)
            {
                return GenerateContacts(toolKey);
            }

            ContactAndListMethods.SetContactInfo(ContactOperations.GenerateContact, contactModel);
            ContactAndListMethods.GenerateContacts(contactNum, listNum, toolKey, 50);
            
            return GenerateContacts(toolKey);
        }

        

    }
}
