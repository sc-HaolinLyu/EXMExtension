using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EXMExtension.Models;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;

namespace EXMExtension
{
    public class InternalMethods
    {
        public static bool GenerateContacts()
        {
            return false;
        }

        public static bool RemoveContacts()
        {
            return false;
        }

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


    }
}
