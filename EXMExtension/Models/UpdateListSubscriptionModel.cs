using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.XConnect;

namespace EXMExtension.Models
{
    /* <summary>
    The following param could be used:

    message.MessageId: The email campaign id

    message.ManagerRootId: The id of the manager root associated with the email campaign

    message.UnsubscribeInteractionData.MessageLanguage: The language the contact receives the email campaign in

    message.ContactIdentifier: The identifier of the contact

    message.ListId: The specific list you want to subscribe or unsubscribe
     </summary>*/
    public class UpdateListSubscriptionModel:EmailAndContactListModel
    {
        public string ManagerRootId;

        public string MessageId;

        public string ContactIdentifierValue;

        public string ContactIdentifierKey;

        public string ListId;

        public UpdateListSubscriptionModel()
        {
            ErrorList = new List<string>();
        }

        public override void Reset()
        {
            ManagerRootId = "";

            MessageId = "";

            ContactIdentifierKey = "";

            ContactIdentifierValue = "";

            ListId = "";

            ErrorList.Clear();
        }
    }
}
