using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EXMExtension.Models;
using Sitecore.Common;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;
using Sitecore.XConnect;
using Sitecore.XConnect.Client;
using Sitecore.XConnect.Collection.Model;


namespace EXMExtension
{
    public class InternalMethods
    {
        public static void GenerateContacts(int contactNum, string toolKey, int batchSize = 50)
        {
            
            Task contactTask = new Task(async delegate
            {

                TaskWrapper task= null;
                if (ExmToolGlobalModel.ActiveTasks.ContainsKey(toolKey))
                    task = ExmToolGlobalModel.ActiveTasks[toolKey];
                using (XConnectClient client =
                       Sitecore.XConnect.Client.Configuration.SitecoreXConnectClientConfiguration.GetClient())
                {
                    try
                    {
                        while (contactNum > 0)
                        {
                            contactNum -= batchSize;
                            if (contactNum < batchSize)
                                batchSize = contactNum;
                            for (int i = 0; i < batchSize; i++)
                            {
                                var curContact = new Sitecore.XConnect.Contact();
                                SetContactIdentifier(client,curContact);
                                SetContactFacetAndUpdateModel(client, curContact, task);
                                client.AddContact(curContact);
                            }

                            await client.SubmitAsync();
                        }
                    }
                    catch (XdbExecutionException ex)
                    {

                        task.model.Reset();
                        task.model.errorList.Add("XConnect Error during the contact adding process "+ex.Message);
                        task.task = null;
                    }
                }
            });
            contactTask.ContinueWith(delegate
            {
                if (ExmToolGlobalModel.ActiveTasks.ContainsKey(toolKey))
                {
                    var task = ExmToolGlobalModel.ActiveTasks[toolKey];
                    task.model.Reset();
                    task.task = null;
                }
                    
            });
            if (!ExmToolGlobalModel.ActiveTasks.ContainsKey(toolKey))
                ExmToolGlobalModel.ActiveTasks.Add(toolKey, new TaskWrapper()
                    {
                        model = new GenerateContactModel()
                        {
                            _task = contactTask,
                            currentProgress = 0,
                            errorList = new List<string>(),
                            isActive = true,
                            success = false,
                            target = contactNum
                        },
                        task = contactTask,
                    }
                );
            else
            {
                var task = ExmToolGlobalModel.ActiveTasks[toolKey];
                task.task = contactTask;
                task.model = new GenerateContactModel()
                {
                    _task = contactTask,
                    currentProgress = 0,
                    errorList = new List<string>(),
                    isActive = true,
                    success = false,
                    target = contactNum
                };

            }
            
            contactTask.Start();
        }
    

        

        public static bool RemoveContacts()
        {
            return false;
        }

        

        /// <summary>
        /// Set up random facet for personal name and email address
        /// </summary>
        /// <param name="contact"></param>
        private static void SetContactFacetAndUpdateModel(XConnectClient client, Contact contact, TaskWrapper task)
        {
            
            PersonalInformation personalInfoFacet = new PersonalInformation()
            {
                FirstName = "ExmToolFirst"+GetRandomNum(),
                LastName = "ExmLast"+ GetRandomNum()
            };

            FacetReference reference = new FacetReference(contact, PersonalInformation.DefaultFacetKey);

            client.SetFacet(reference, personalInfoFacet);

            string emailAddr = "exmTool" + GetRandomNum()+"@"+ GetRandomNum()+".com";
            // Facet without a reference, using default key
            EmailAddressList emails = new EmailAddressList(new EmailAddress(emailAddr, true), "Home");

            client.SetFacet(contact, emails);
            if(task!=null)
                task.model.currentProgress++;
        }

        private static void SetContactIdentifier(XConnectClient client, Contact contact)
        {
            var identifier = new ContactIdentifier("ExmTool", "ExmTool" + GetRandomNum(), ContactIdentifierType.Known);
            client.AddContactIdentifier(contact, identifier);
        }
        
        private static int GetRandomNum()
        {
            var seed = Guid.NewGuid().GetHashCode();
            Random rnd = new Random(seed);
            return rnd.Next(0, int.MaxValue);
        }

        /// <summary>
        /// Initialize Tool dictionary
        /// </summary>
        public static void InitializeToolDictionary()
        {
            var toolList = Factory.GetConfigNode("ExmTools/ToolList").ChildNodes;
            for (int i = 0; i < toolList.Count; i++)
            {

                var tool = toolList[i];
                var name = tool.SelectSingleNode("Name")?.InnerText;
                var sequence = tool.SelectSingleNode("Sequence")?.InnerText;
                var action = tool.SelectSingleNode("Action")?.InnerText;
                var description = tool.SelectSingleNode("Description")?.InnerText;
                int seq = 0;
                var addedTool = new Tool()
                {
                    Name = name.IsNullOrEmpty() ? "Undefined" : name,
                    Action = action,
                    Sequence = int.TryParse(sequence, out seq) ? seq : 0,
                    Description = description
                };
                if (!ExmToolGlobalModel.ToolMapping.ContainsKey(name))
                    ExmToolGlobalModel.ToolMapping.Add(name, addedTool);
            }
        }

        public static T2 ReadValueFromDictionary<T1, T2>(Dictionary<T1, T2> dic, T1 key)
        {
            if (!dic.ContainsKey(key))
                return default(T2);
            return dic[key];
        }





    }
}
