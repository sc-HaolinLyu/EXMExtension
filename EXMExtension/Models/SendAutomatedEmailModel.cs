using System.Collections.Generic;
using EXMExtension.Models;

namespace EXMExtension.Models
{
    public class SendAutomatedEmailModel : EmailAndContactListModel
    {
        public string MessageId;

        public string IdentifierSource;

        public string IdentifierValue;

        public SendAutomatedEmailModel(string _toolName)
        {
            ToolName = _toolName;
            ErrorList = new List<string>();
        }

        public override void Reset()
        {
            MessageId = "";
            IdentifierSource = "";
            IdentifierValue = "";
            ErrorList.Clear();
        }
    }
}