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
                                Log.Info("The current progress "+model.CurrentProgressContact+"|"+model.CurrentProgressList+"|"+contactNumTemp+"|"+batchsizeTemp, new object());
                                client.Submit();
                                AddContactsToList(id, generatedContacts);
                            }

                            model.CurrentProgressContact = 0;
                            model.CurrentProgressList++;
                        }
                    }
                    catch (XdbExecutionException ex)
                    {
                        model.Reset();
                        model.ErrorList.Add("XConnect Error during the contact adding process " + ex.Message);
                        model.Task = null;
                    }
                    catch (Exception ex)
                    {
                        model.Reset();
                        model.ErrorList.Add("General Error during the contact adding process " + ex.Message);
                        model.Task = null;
                    }
                }
            });
            contactTask.ContinueWith(delegate
            {

                var model = ExmToolGlobalModel.ActiveTasks[toolKey] as GenerateContactModel;
                model?.Reset();
                model.Task = null;
            });
            var task = ExmToolGlobalModel.ActiveTasks[toolKey] as GenerateContactModel;
            task.CurrentProgressContact = 0;
            task.CurrentProgressList = 0;
            task.ErrorList = new List<string>();
            task.IsActive = true;
            task.TargetContact = contactNum;
            task.TargetList = listNum;
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

                            model.CurrentProgressContact += enumerator.Current.Count;
                        }
                    }
                    catch (Exception e)
                    {
                        model.Reset();
                        model.ErrorList.Add("The contacts were failed to be removed");
                    }
                }
            });
            removeTask.ContinueWith(delegate
            {
                model.Reset();
            });
            model.Reset();
            model.IsActive = true;
            model.Current = ContactOperations.RemoveContact;
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
                model.ErrorList.Add("The fake lists were failed to be removed");
            }
            

        }


        public static void SetContactInfo(ContactOperations operation, GenerateContactModel model)
        {
            switch (operation)
            {
                case ContactOperations.GenerateContact:
                {
                    model.CurrentInfo[0] = "the current generated contacts:";
                    model.CurrentInfo[1] = "the current generated lists:";
                    model.TargetInfo[0] = "The total contacts that will be generated:";
                    model.TargetInfo[1] = "The total lists that will be generated:";
                    model.Title = "There is an active contact generation task right now, Please wait for a while";
                    model.Current = ContactOperations.GenerateContact;
                    break;
                }
                case ContactOperations.RemoveContact:
                {
                    model.CurrentInfo[0] = "The total contacts that will be removed:";
                    model.TargetInfo[0] = "The current removed contacts:";
                    model.Title = "There is an active contact purging task right now, Please wait for a while";
                    model.Current = ContactOperations.RemoveContact;
                        break;
                }
                default:
                    break;
            }
        }

        /// <summary>
        /// Pick up the first batch and choose a random contact inside and return it.
        /// Returns null if no contact is found
        /// </summary>
        /// <returns></returns>
        public static Contact PickContact(EmailAndContactListModel model)
        {
            try
            {
                using (XConnectClient client =
                       Sitecore.XConnect.Client.Configuration.SitecoreXConnectClientConfiguration.GetClient())
                {
                    var queryable = client.Contacts.Where(c => c.Identifiers.Any(s => s.Source == "ExmTool"));
                    var enumerable = queryable.GetBatchEnumeratorSync(100);
                    List<Contact> candidates = new List<Contact>();
                    if (enumerable.MoveNext())
                        foreach (var c in enumerable.Current)
                        {
                            candidates.Add(c);
                        }

                    if (candidates.Count > 0)
                    {
                        int index = new Random().Next(candidates.Count);
                        return candidates[index];
                    }

                    return null;
                }
            }
            catch (Exception e)
            {
                model.ErrorList.Add(e.Message);
                return null;
            }
            
            
        }

        public static ContactListModel PickList(EmailAndContactListModel model)
        {
            var contactListRepository = ServiceLocator.ServiceProvider.GetService<IFetchRepository<ContactListModel>>();
            try
            {
                using (var lists = contactListRepository.GetAll().Where(l => l.Name.Contains("ExmTool")).GetEnumerator())
                {
                    var listsList = new List<ContactListModel>();
                    while (lists.MoveNext())
                    {
                        listsList.Add(lists.Current);
                    }

                    if (listsList.Count > 0)
                    {
                        int index = new Random().Next(listsList.Count);
                        return listsList[index];
                    }

                    return null;
                }
            }
            catch (Exception e)
            {
                model.ErrorList.Add(e.Message);
                return null;
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
                model.CurrentProgressContact++;
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
