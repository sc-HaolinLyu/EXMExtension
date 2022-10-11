using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;

namespace EXMExtension.Models
{
    public class ExmToolGlobalModel
    {
        public static Dictionary<string, Tool> ToolMapping;

        private object obj = new object();
        public ExmToolGlobalModel()
        {
            if (ToolMapping == null)
            {
                lock (obj)
                {
                    if (ToolMapping == null)
                    {
                        ToolMapping = new Dictionary<string, Tool>();
                        InternalMethods.InitializeToolDictionary();
                    }
                }
            }
        }
    }

    public class Tool
    {
        public string Name;

        public int Sequence;

        public string Action;

        public string Description;
    }
}
