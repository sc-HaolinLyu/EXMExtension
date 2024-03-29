﻿using System;
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
using Sitecore.EmailCampaign.Model.Messaging;
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
        }
        
        [System.Web.Http.HttpGet]
        public ActionResult Index()
        {
            model.ActiveToolName = "Home";
            this.ViewData["ExmToolName"] = model.ActiveToolName;
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
            model.ActiveToolName = toolKey;
            this.ViewData["ExmToolName"] = model.ActiveToolName;
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
            model.ActiveToolName = toolKey;
            this.ViewData["ExmToolName"] = model.ActiveToolName;
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
            model.ActiveToolName = toolKey;
            this.ViewData["ExmToolName"] = model.ActiveToolName;
            if (!ExmToolGlobalModel.ActiveTasks.ContainsKey(toolKey))
            {
                ExmToolGlobalModel.ActiveTasks.Add(toolKey, new SendAutomatedEmailModel(toolKey));
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
            if (sendAutomatedEmailModel.ErrorList.Count>0)
                return SendAutomatedEmail(toolKey);
            ExmMethods.SendAutomatedEmail(sendAutomatedEmailModel);
            return SendAutomatedEmail(toolKey);
        }

        /// <summary>
        /// Try to pick up a random contact, list information on every refresh
        /// </summary>
        /// <returns></returns>
        [System.Web.Mvc.AcceptVerbs(HttpVerbs.Get)]
        public ActionResult PickUpContactAndList(string toolKey = "PickUpContactAndList")
        {
            model.ActiveToolName = "PickUpContactAndList";
            this.ViewData["ExmToolName"] = model.ActiveToolName;
            if (!ExmToolGlobalModel.ActiveTasks.ContainsKey(toolKey))
            {
                ExmToolGlobalModel.ActiveTasks.Add(toolKey, new PickUpContactAndListModel(toolKey));
            }
            var pickUpContactAndListModel = ExmToolGlobalModel.ActiveTasks[toolKey] as PickUpContactAndListModel;
            pickUpContactAndListModel.Reset();
            var sampleContact = ContactAndListMethods.PickContact(pickUpContactAndListModel);
            var sampleList = ContactAndListMethods.PickList(pickUpContactAndListModel);
            if (sampleContact == null)
            {
                pickUpContactAndListModel.ErrorList.Add("No contact is found!");
            }
            if (sampleList == null)
            {
                pickUpContactAndListModel.ErrorList.Add("No list is found!");
            }

            if (pickUpContactAndListModel.ErrorList.Count > 0)
            {
                return View("~/Views/ExmTools/PickUpContactAndList.cshtml", pickUpContactAndListModel);
            }
            //Add values to the model
            pickUpContactAndListModel.IdentifierSource = "ExmTool";
            pickUpContactAndListModel.IdentifierValue =
                sampleContact.Identifiers.First(i => i.Source == "ExmTool")?.Identifier;
            pickUpContactAndListModel.ListId = sampleList.Id;
            pickUpContactAndListModel.ListName = sampleList.Name;
            return View("~/Views/ExmTools/PickUpContactAndList.cshtml", pickUpContactAndListModel);
        }

        [System.Web.Mvc.AcceptVerbs(HttpVerbs.Get)]
        public ActionResult UpdateListSubscription(string toolKey = "UpdateListSubscription")
        {
            model.ActiveToolName = toolKey;
            this.ViewData["ExmToolName"] = model.ActiveToolName;
            if (!ExmToolGlobalModel.ActiveTasks.ContainsKey(toolKey))
            {
                ExmToolGlobalModel.ActiveTasks.Add(toolKey, new UpdateListSubscriptionModel());
            }
            var updateListSubscriptionModel = ExmToolGlobalModel.ActiveTasks[toolKey] as UpdateListSubscriptionModel;
            return View("~/Views/ExmTools/UpdateListSubscription.cshtml", updateListSubscriptionModel);

        }

        public ActionResult DispatchSummary(string toolKey = "DispatchSummary")
        {
            model.ActiveToolName = toolKey;
            this.ViewData["ExmToolName"] = model.ActiveToolName;
            if (!ExmToolGlobalModel.ActiveTasks.ContainsKey(toolKey))
            {
                ExmToolGlobalModel.ActiveTasks.Add(toolKey, new DispatchSummaryModel());
            }
            var dispatchSummaryModel = ExmToolGlobalModel.ActiveTasks[toolKey] as DispatchSummaryModel;
            return View("~/Views/ExmTools/DispatchSummary.cshtml", dispatchSummaryModel);
        }

        [System.Web.Mvc.AcceptVerbs(HttpVerbs.Post)]
        public ActionResult UpdateListSubscription()
        {
            string toolKey = "UpdateListSubscription";
            string dummyGuid = "00000000-0000-0000-0000-000000000000";
            var updateListSubscriptionModel = ExmToolGlobalModel.ActiveTasks[toolKey] as UpdateListSubscriptionModel;
            var updateListOperation = Request.Form["listOperation"];
            var managerRootId = Request.Form["managerRootId"];
            var messageId = Request.Form["messageId"];
            var contactIdentifierSource = Request.Form["identifierSource"];
            var contactIdentifierValue = Request.Form["identifierValue"];
            var operation = ListSubscribeOperation.Subscribe;
            var listId = Request.Form["listId"];
            Log.Info("Debug: The values "+ updateListOperation+ managerRootId+ messageId+ contactIdentifierSource+contactIdentifierValue, this);
            updateListSubscriptionModel.Reset();
            if (string.IsNullOrEmpty(updateListOperation))
            {
                updateListSubscriptionModel.ErrorList.Add("The update list operation is not clear");
                return UpdateListSubscription(toolKey);
            }
            if (updateListOperation[0] < '0' || updateListOperation[0] > '4')
            {
                updateListSubscriptionModel.ErrorList.Add("The update list operation is not clear");
                return UpdateListSubscription(toolKey);
            }
            /*
             0: subscribe to specific email campaign
             1: unsubscribe from specific email campaign
             2: unsubscribe from all email campaign and put it in global opt-out list
             3: add to specific list
             4: remove from specific list
             */
            switch (updateListOperation[0])
            {
                case '0':
                case '1':
                    {
                    if (string.IsNullOrEmpty(messageId) || string.IsNullOrEmpty(contactIdentifierSource) ||
                        string.IsNullOrEmpty(contactIdentifierValue))
                    {
                        updateListSubscriptionModel.ErrorList.Add(
                            "The message id, contact identifier information should be provided");
                    }

                    operation = updateListOperation[0] == '0'
                        ? ListSubscribeOperation.Subscribe
                        : ListSubscribeOperation.Unsubscribe;
                    managerRootId = dummyGuid;
                    listId = dummyGuid;
                    break;
                }
                case '2':
                {
                    if (string.IsNullOrEmpty(managerRootId) || string.IsNullOrEmpty(contactIdentifierSource) ||
                        string.IsNullOrEmpty(contactIdentifierValue))
                    {
                        updateListSubscriptionModel.ErrorList.Add(
                            "The managerRoot id, contact identifier information should be provided");
                    }

                    operation = ListSubscribeOperation.UnsubscribeFromAll;
                    listId = dummyGuid;
                    messageId = dummyGuid;
                    break;
                }
                case '3':
                case '4':
                {
                    if (string.IsNullOrEmpty(listId) || string.IsNullOrEmpty(contactIdentifierSource) ||
                        string.IsNullOrEmpty(contactIdentifierValue))
                    {
                        updateListSubscriptionModel.ErrorList.Add(
                            "The list id, contact identifier information should be provided");
                    }

                    operation = updateListOperation[0] == '3'
                        ? ListSubscribeOperation.AddToList
                        : ListSubscribeOperation.RemoveFromList;
                    messageId = dummyGuid;
                    managerRootId = dummyGuid;
                    break;
                }
                default:
                {
                    updateListSubscriptionModel.ErrorList.Add(
                        "unknown operation");
                    break;
                }
            }

            if(updateListSubscriptionModel.ErrorList.Count>0)
                return UpdateListSubscription(toolKey);

            //Everything looks good.
            try
            {
                ExmMethods.UpdateListSubscription(operation, contactIdentifierValue, contactIdentifierSource, listId,
                    messageId, managerRootId);
            }
            catch (Exception ex)
            {
                updateListSubscriptionModel.ErrorList.Add("There is error during list operation " + ex.Message);
            }
            return UpdateListSubscription(toolKey);

        }





        private ActionResult RemoveContact()
        {
            string toolKey = "GenerateContacts";
            var contactModel = ExmToolGlobalModel.ActiveTasks[toolKey] as GenerateContactModel;
            contactModel.Current = ContactOperations.RemoveContact;
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
            contactModel.Current = ContactOperations.GenerateContact;
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

            ContactAndListMethods.GenerateContacts(contactNum, listNum, toolKey, 50);
            
            return GenerateContacts(toolKey);
        }

        

    }
}
