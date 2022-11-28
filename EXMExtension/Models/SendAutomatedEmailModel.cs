using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EXMExtension.Models
{
    public class SendAutomatedEmailModel:BaseTaskWrapper
    {
        public string MessageId;

        public string IdentifierSource;

        public string IdentifierValue;

        public SendAutomatedEmailModel()
        {
            ErrorList = new List<string>();
        }

        public SendAutomatedEmailModel(string _messageId, string _identifierSource, string _identifierValue)
        {
            MessageId = _messageId;
            IdentifierSource = _identifierSource;
            IdentifierValue = _identifierValue;
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
