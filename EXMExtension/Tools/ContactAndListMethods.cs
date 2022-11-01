using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EXMExtension.Models;
using Sitecore.Common;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;
using Sitecore.XConnect;
using Sitecore.XConnect.Client;
using Sitecore.XConnect.Collection.Model;
using Sitecore.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.ListManagement.Services.Repositories;
using Sitecore.ListManagement.Services.Model;
using Sitecore.ListManagement.XConnect.Web;






namespace EXMExtension.Tools
{
    public enum ContactOperations
    {
        GenerateContact=1,
        RemoveContact = 2,
        Idle = 0
    }
    
    public static class ContactAndListMethods
    {
        private static ISubscriptionService _subscriptionService = ServiceLocator.ServiceProvider.GetService<ISubscriptionService>();

        public static void GenerateContacts(int contactNum, int listNum, string toolKey, int batchSize = 50)
        {

            Task contactTask = new Task(delegate
            {

                var model = ExmToolGlobalModel.ActiveTasks[toolKey] as GenerateContactModel;
                using (XConnectClient client =
                       Sitecore.XConnect.Client.Configuration.SitecoreXConnectClientConfiguration.GetClient())
                {
                    try
                    {
                        for (int list = 0; list < listNum; list++)
                        {
                            int contactNumTemp = contactNum;
                            int batchsizeTemp = batchSize;
                            Log.Info("The current listNum"+list, new object());
                            string id = CreateContactList();
                            Log.Info("Debug: The current list id "+id, new object());
                            while (contactNumTemp > 0)
                            {
                                List<Contact> generatedContacts = new List<Contact>();
                                if (contactNumTemp < batchsizeTemp)
                                    batchsizeTemp = contactNumTemp;
                                contactNumTemp -= batchsizeTemp;
                                for (int i = 0; i < batchsizeTemp; i++)
                                {
                                    var curContact = new Contact();
                                    SetContactIdentifier(client, curContact);
                                    SetContactFacetAndUpdateModel(client, curContact, model);
                                    client.AddContact(curContact);
                                    generatedContacts.Add(curContact);
                                }
                                Thread.Sleep(200);
                                Log.Info("The current progress "+model.currentProgressContact+"|"+model.currentProgressList+"|"+contactNumTemp+"|"+batchsizeTemp, new object());
                                client.Submit();
                                AddContactsToList(id, generatedContacts);
                            }

                            model.currentProgressContact = 0;
                            model.currentProgressList++;
                        }
                    }
                    catch (XdbExecutionException ex)
                    {
                        model.Reset();
                        model.errorList.Add("XConnect Error during the contact adding process " + ex.Message);
                        model.task = null;
                    }
                    catch (Exception ex)
                    {
                        model.Reset();
                        model.errorList.Add("General Error during the contact adding process " + ex.Message);
                        model.task = null;
                    }
                }
            });
            contactTask.ContinueWith(delegate
            {

                var model = ExmToolGlobalModel.ActiveTasks[toolKey] as GenerateContactModel;
                model?.Reset();
                model.task = null;
            });
            var task = ExmToolGlobalModel.ActiveTasks[toolKey] as GenerateContactModel;
            task.currentProgressContact = 0;
            task.currentProgressList = 0;
            task.errorList = new List<string>();
            task.isActive = true;
            task.targetContact = contactNum;
            task.targetList = listNum;
            contactTask.Start();
        }


        public static void RemoveContacts(string toolKey, int batchSize = 50)
        {
            var model = ExmToolGlobalModel.ActiveTasks[toolKey] as GenerateContactModel;
            Task removeTask = new Task(delegate
            {
                using (Sitecore.XConnect.Client.XConnectClient client =
                       Sitecore.XConnect.Client.Configuration.SitecoreXConnectClientConfiguration.GetClient())
                {
                    try
                    {


                        var queryable = client.Contacts.Where(c => c.Identifiers.Any(s => s.Source == "ExmTool"));
                        var enumerator = queryable.GetBatchEnumeratorSync(100);
                        int counter = 0;

                        // Cycle through batches
                        while (enumerator.MoveNext())
                        {
                            counter = counter + enumerator.Current.Count;
                            foreach (var contact in enumerator.Current)
                            {
                                client.DeleteContact(contact);
                            }

                            if (counter == 100)
                            {
                                client.Submit();
                                counter = 0;
                            }

                            model.currentProgressContact += enumerator.Current.Count;
                        }
                    }
                    catch (Exception e)
                    {
                        model.Reset();
                        model.errorList.Add("The contacts were failed to be removed");
                    }
                }
            });
            removeTask.ContinueWith(delegate
            {
                model.Reset();
            });
            model.Reset();
            model.isActive = true;
            model.current = ContactOperations.RemoveContact;
            removeTask.Start();
        }

        public static void RemoveLists(string toolKey)
        {
            var model = ExmToolGlobalModel.ActiveTasks[toolKey] as GenerateContactModel;
            var contactListRepository = ServiceLocator.ServiceProvider.GetService<IFetchRepository<ContactListModel>>();
            try
            {
                using (var lists = contactListRepository.GetAll().Where(l => l.Name.Contains("ExmTool")).GetEnumerator())
                {
                    while (lists.MoveNext())
                    {
                        contactListRepository.Delete(lists.Current);
                    }
                }
            }
            catch (Exception e)
            {
                model.errorList.Add("The fake lists were failed to be removed");
            }
            

        }


        public static void SetContactInfo(ContactOperations operation, GenerateContactModel model)
        {
            switch (operation)
            {
                case ContactOperations.GenerateContact:
                {
                    model.currentInfo[0] = "the current generated contacts:";
                    model.currentInfo[1] = "the current generated lists:";
                    model.targetInfo[0] = "The total contacts that will be generated:";
                    model.targetInfo[1] = "The total lists that will be generated:";
                    model.title = "There is an active contact generation task right now, Please wait for a while";
                    model.current = ContactOperations.GenerateContact;
                    break;
                }
                case ContactOperations.RemoveContact:
                {
                    model.currentInfo[0] = "The total contacts that will be removed:";
                    model.targetInfo[0] = "The current removed contacts:";
                    model.title = "There is an active contact purging task right now, Please wait for a while";
                    model.current = ContactOperations.RemoveContact;
                        break;
                }
                default:
                    break;
            }
        }



        /// <summary>
        /// Set up random facet for personal name and email address
        /// </summary>
        /// <param name="contact"></param>
        private static void SetContactFacetAndUpdateModel(XConnectClient client, Contact contact, GenerateContactModel model)
        {

            PersonalInformation personalInfoFacet = new PersonalInformation()
            {
                FirstName = "ExmToolFirst" + GeneralMethods.GetRandomNum(),
                LastName = "ExmLast" + GeneralMethods.GetRandomNum(),
            };

            FacetReference reference = new FacetReference(contact, PersonalInformation.DefaultFacetKey);

            client.SetFacet(reference, personalInfoFacet);

            string emailAddr = "exmTool" + GeneralMethods.GetRandomNum() + "@" + GeneralMethods.GetRandomNum() + ".com";
            // Facet without a reference, using default key
            EmailAddressList emails = new EmailAddressList(new EmailAddress(emailAddr, true), "Home");

            client.SetFacet(contact, emails);
            if (model != null)
                model.currentProgressContact++;
        }

        private static void SetContactIdentifier(XConnectClient client, Contact contact)
        {
            var identifier = new ContactIdentifier("ExmTool", "ExmTool" + GeneralMethods.GetRandomNum(), ContactIdentifierType.Known);
            client.AddContactIdentifier(contact, identifier);
        }

        private static string CreateContactList()
        {
            var contactListRepository = ServiceLocator.ServiceProvider.GetService<IFetchRepository<ContactListModel>>();
            var newContactList = new ContactListModel()
            {
                Id = Guid.NewGuid().ToString("B"),
                Name = "ExmTool"+GeneralMethods.GetRandomNum(),
                Description = "The Generated List with fake contacts",
            };
            contactListRepository.Add(newContactList);
            //Log.Info("The current contact list id and item id"+ newContactList.Id+" | "+newContactList.itemId, new object());
            return newContactList.Id;
        }

        private static void AddContactsToList(string id, List<Contact> contacts)
        {
            _subscriptionService.Subscribe(new Guid(id), contacts);
        }

        
    }
}
