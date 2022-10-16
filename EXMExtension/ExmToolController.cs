using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Http;
using EXMExtension.Models;
using Sitecore;
using Sitecore.localhost;
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
        [System.Web.Http.HttpGet]
        public ActionResult GenerateContacts(GenerateContactModel model = null, string toolKey = "GenerateContact")
        {
            var modelWrapper =
                InternalMethods.ReadValueFromDictionary(ExmToolGlobalModel.ActiveTasks, toolKey);
            if (modelWrapper == null) // first request
            {
                TaskWrapper task = new TaskWrapper();
                ExmToolGlobalModel.ActiveTasks.Add(toolKey, task);
                modelWrapper = task;
            }
            return View("~/Views/ExmTools/GenerateContacts.cshtml", modelWrapper.model);
        }

        /// <summary>
        /// The post request takes in the form model.
        /// 1. Do the input validation. If the params are not correct, return the error in the model
        /// 2. Create the task and store it in the model
        /// 3. Based on the task result. Push the error in the error model or just show success information and show the form again
        /// </summary>
        /// <returns></returns>
        [System.Web.Http.HttpPost]
        public ActionResult GeneratingData([FromBody] ContactFormModel inputData)
        {
            var generateContactModel =
                InternalMethods.ReadValueFromDictionary(ExmToolGlobalModel.ActiveTasks, inputData.toolKey).model;
            int contactNum = -1;
            var contactInfo = inputData.ContactCreationNum?.Split(',');
            if (contactInfo.Length != 2)
            {
                generateContactModel.errorList.Add("contactField", "The parameter contact is not correct");
            }
            else
            {
                int.TryParse(contactInfo[1], out contactNum);
                if (contactNum <= 0 || contactNum > 10000)
                {
                    generateContactModel.errorList.Add("contactField", "The parameter contact is not correct");
                }
            }

            if (generateContactModel.errorList.Count > 0)
            {
                return GenerateContacts(generateContactModel);
            }

            InternalMethods.GenerateContacts(contactNum, inputData.toolKey, 50);
            
            return GenerateContacts();
        }

        

    }
}
