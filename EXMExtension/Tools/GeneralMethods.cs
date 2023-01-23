using System;
using System.Collections.Concurrent;
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


namespace EXMExtension.Tools
{
    public class GeneralMethods
    {
        
        public static int GetRandomNum()
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

        public static string ConvertToString(double value)
        {
            int num = (int)Math.Round(value);
            if (num != 0)
            {
                return num.ToString();
            }
            return Math.Round(value, 3).ToString();
        }

        public static T2 ReadValueFromDictionary<T1, T2>(Dictionary<T1, T2> dic, T1 key) where T2 : new()
        {
            if (!dic.ContainsKey(key))
            {
                dic.Add(key, new T2());
                return dic[key];
            }

            return dic[key];
        }

    }
}
