using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EXMExtension.Tools;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;

namespace EXMExtension.Models
{
    public class ExmToolGlobalModel
    {
        public static Dictionary<string, Tool> ToolMapping;

        public static Dictionary<string, BaseTaskWrapper> ActiveTasks;

        private string _activeToolName;

        public string ActiveToolName
        {
            get
            {
                if (string.IsNullOrEmpty(_activeToolName))
                {
                    return "Home";
                }

                return _activeToolName;
            }
            set
            {
                _activeToolName = value;
            }
        }

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
                        GeneralMethods.InitializeToolDictionary();
                    }
                }
            }

            if (ActiveTasks == null)
            {
                lock (obj)
                {
                    if (ActiveTasks == null)
                    {
                        ActiveTasks = new Dictionary<string, BaseTaskWrapper>();
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
