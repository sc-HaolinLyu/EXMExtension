using EXMExtension.Models;

namespace EXMExtension.Models
{
    public class PickUpContactAndListModel : EmailAndContactListModel
    {
        public string IdentifierSource;

        public string IdentifierValue;

        public string ListId;

        public string ListName;

        public PickUpContactAndListModel(string _toolName)
        {
            ToolName = _toolName;
        }

        public override void Reset()
        {
            IdentifierSource = "";
            IdentifierValue = "";
            ListId = "";
            ListName = "";
            ToolName = "";
        }
    }
}