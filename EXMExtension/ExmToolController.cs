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

        /// <summary>
        /// Initialize the auth and crypto key based on the connection string. Set it as invalid if it doesn't exist
        /// </summary>
        /// <param name="toolKey"></param>
        /// <returns></returns>
        [System.Web.Mvc.AcceptVerbs(HttpVerbs.Get)]
        public ActionResult DecryptEmailLink(string toolKey = "DecryptEmailLink")
        {
            if (!ExmToolGlobalModel.ActiveTasks.ContainsKey(toolKey))
            {
                ExmToolGlobalModel.ActiveTasks.Add(toolKey, new DecryptEmailLinkModel());
                ExmMethods.InitializeKeys();
            }
            var decryptEmailLinkModel = ExmToolGlobalModel.ActiveTasks[toolKey] as DecryptEmailLinkModel;
            if(string.Equals(DecryptEmailLinkModel.AuthKey, "invalid")|| string.Equals(DecryptEmailLinkModel.CryptoKey, "invalid"))
                decryptEmailLinkModel.ErrorList.Add("The connectionstring auth key or crypto key is invalid");
            return View("~/Views/ExmTools/DecryptEmailLink.cshtml", decryptEmailLinkModel);
        }

        [System.Web.Mvc.AcceptVerbs(HttpVerbs.Post)]
        public ActionResult DecryptEmailLink()
        {
            string toolKey = "DecryptEmailLink";
            var decrptyEmailLinkModel = ExmToolGlobalModel.ActiveTasks[toolKey] as DecryptEmailLinkModel;
            decrptyEmailLinkModel.Reset();
            decrptyEmailLinkModel.QueryString = Request.Form["queryString"];
            decrptyEmailLinkModel.OverrideAuthKey = Request.Form["authKey"];
            decrptyEmailLinkModel.OverrideCryptoKey = Request.Form["cryptoKey"];
            if (decrptyEmailLinkModel.OverrideAuthKey.Length < 64)
            {
                if(string.Equals(DecryptEmailLinkModel.AuthKey, "invalid"))
                    decrptyEmailLinkModel.ErrorList.Add("There is no valid authKey avaliable");
                else
                    decrptyEmailLinkModel.ErrorList.Add("The override authKey is invalid, use the default auth key");
                decrptyEmailLinkModel.OverrideAuthKey = DecryptEmailLinkModel.AuthKey;
            }
            if (decrptyEmailLinkModel.OverrideCryptoKey.Length < 64)
            {
                if(string.Equals(DecryptEmailLinkModel.CryptoKey, "invalid"))
                    decrptyEmailLinkModel.ErrorList.Add("There is no valid cryptoKey avaliable");
                else
                    decrptyEmailLinkModel.ErrorList.Add(
                        "The override cryptoKey is invalid, use the default crypto key");
                decrptyEmailLinkModel.OverrideCryptoKey = DecryptEmailLinkModel.CryptoKey;
            }

            if (string.Equals(decrptyEmailLinkModel.OverrideAuthKey, "invalid") ||
                string.Equals(decrptyEmailLinkModel.OverrideCryptoKey, "invalid"))
                return DecryptEmailLink("DecryptEmailLink");
            decrptyEmailLinkModel.DecryptionResult = ExmMethods.DecryptQueryString(decrptyEmailLinkModel.OverrideCryptoKey,
                decrptyEmailLinkModel.OverrideAuthKey, decrptyEmailLinkModel.QueryString);
            if (string.IsNullOrEmpty(decrptyEmailLinkModel.DecryptionResult))
                decrptyEmailLinkModel.ErrorList.Add("decrypt failed");
            return DecryptEmailLink("DecryptEmailLink");
        }

        /// <summary>
        /// Initialize the Model in the ActiveTasks list. Then generate the form.
        /// </summary>
        /// <param name="toolKey"></param>
        /// <returns></returns>
        [System.Web.Mvc.AcceptVerbs(HttpVerbs.Get)]
        public ActionResult SendAutomatedEmail(string toolKey = "SendAutomatedEmail")
        {
            if (!ExmToolGlobalModel.ActiveTasks.ContainsKey(toolKey))
            {
                ExmToolGlobalModel.ActiveTasks.Add(toolKey, new SendAutomatedEmailModel());
            }
            var sendAutomatedEmailModel = ExmToolGlobalModel.ActiveTasks[toolKey] as SendAutomatedEmailModel;
            return View("~/Views/ExmTools/SendAutomatedEmail.cshtml", sendAutomatedEmailModel);
        }

        /// <summary>
        /// Try to send the automated email campaign and regenerate the form.
        /// </summary>
        /// <returns></returns>
        [System.Web.Mvc.AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SendAutomatedEmail()
        {
            string toolKey = "SendAutomatedEmail";
            var sendAutomatedEmailModel = ExmToolGlobalModel.ActiveTasks[toolKey] as SendAutomatedEmailModel;
            sendAutomatedEmailModel.Reset();
            sendAutomatedEmailModel.MessageId = Request.Form["messageId"];
            sendAutomatedEmailModel.IdentifierSource = Request.Form["identifierSource"];
            sendAutomatedEmailModel.IdentifierValue = Request.Form["identifierValue"];
            if(string.IsNullOrEmpty(sendAutomatedEmailModel.MessageId))
                sendAutomatedEmailModel.ErrorList.Add("The automated email id should not be empty");
            if (string.IsNullOrEmpty(sendAutomatedEmailModel.IdentifierSource))
                sendAutomatedEmailModel.ErrorList.Add("The identifier source should not be empty");
            if (string.IsNullOrEmpty(sendAutomatedEmailModel.IdentifierValue))
                sendAutomatedEmailModel.ErrorList.Add("The identifier value should not be empty");
            if(sendAutomatedEmailModel.ErrorList.Count>0)
                return SendAutomatedEmail(toolKey);
            ExmMethods.SendAutomatedEmail(sendAutomatedEmailModel);
            return SendAutomatedEmail(toolKey);
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
            contactModel.ErrorList.Clear();//clear old errors on each submission
            if (!int.TryParse(contactNumInput, out int contactNum))
            {
                contactModel.ErrorList.Add("The contact number can't be parsed");
            }
            else if(contactNum<=0||contactNum>10000)
            {
                contactModel.ErrorList.Add("The contact number should between 1-10000");
            }
            if (!int.TryParse(listNumInput, out int listNum))
            {
                contactModel.ErrorList.Add("The list number can't be parsed");
            }
            else if (listNum <= 0 || listNum > 10)
            {
                contactModel.ErrorList.Add("The list number should between 1-10");
            }
            if (contactModel.IsActive)
            {
                contactModel.ErrorList.Add("There are active task currently");
            }

            if (contactModel.ErrorList.Count > 0)
            {
                return GenerateContacts(toolKey);
            }

            ContactAndListMethods.SetContactInfo(ContactOperations.GenerateContact, contactModel);
            ContactAndListMethods.GenerateContacts(contactNum, listNum, toolKey, 50);
            
            return GenerateContacts(toolKey);
        }

        

    }
}
