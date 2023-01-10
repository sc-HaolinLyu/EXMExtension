using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using EXMExtension.Models;
using Sitecore.Diagnostics;
using Sitecore.ExM.Framework.Diagnostics;
using Sitecore.ExM.Framework.Helpers;
using Sitecore.Framework.Conditions;
using Sitecore.Modules.EmailCampaign.Core.Crypto;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.DependencyInjection;
using Sitecore.EmailCampaign.Model.Messaging;
using Sitecore.ExperienceForms.Processing;
using Sitecore.XConnect;
using Sitecore.XConnect.Client;
using Sitecore.XConnect.Collection.Model;
using Sitecore.EmailCampaign.Cd.Services;



namespace EXMExtension.Tools
{

    
    
    static class ExmMethods
    {
        private static ILogger _exmLogger = new Logger("Sitecore.EXM", "false");

        private static string _cryptoKeyName = "EXM.CryptographicKey";

        private static string _authKeyName = "EXM.AuthenticationKey";

        private static readonly IClientApiService _clientApiService =
            ServiceLocator.ServiceProvider.GetService<IClientApiService>();

        public static void InitializeKeys()
        {
            try
            {
                DecryptEmailLinkModel.AuthKey = Sitecore.Configuration.Settings.GetConnectionString(_authKeyName);
                DecryptEmailLinkModel.CryptoKey = Sitecore.Configuration.Settings.GetConnectionString(_cryptoKeyName);

            }
            catch (Exception e)
            {
                DecryptEmailLinkModel.AuthKey = "invalid";
                DecryptEmailLinkModel.CryptoKey = "invalid";
            }
            
        }

        public static string DecryptQueryString(string cryptoVal, string authVal, string query)
        {

                var cryptoValByte = ArgumentAsHexadecimalToByteArray(cryptoVal, _cryptoKeyName, false, _exmLogger);
                var authValByte = ArgumentAsHexadecimalToByteArray(authVal, _authKeyName, false, _exmLogger);
                IStringCipher cipher = new AuthenticatedAesStringCipher(cryptoValByte, authValByte, _exmLogger);
                var result = cipher.TryDecrypt(query);
                if (!string.IsNullOrEmpty(result))
                    return result;
                //html decode the query to try again.
                query = HttpUtility.UrlDecode(query);
                result = cipher.TryDecrypt(query);
                return result; // decryption fail
        }

        public static void SendAutomatedEmail(SendAutomatedEmailModel model)
        {
            
            ContactIdentifier contactIdentifier = new ContactIdentifier(model.IdentifierSource, model.IdentifierValue, ContactIdentifierType.Known);
            using (IXdbContext context = 
                Sitecore.XConnect.Client.Configuration.SitecoreXConnectClientConfiguration.GetClient())
            {
                ContactExpandOptions expandOptions = new ContactExpandOptions(EmailAddressList.DefaultFacetKey);
                var contact = context.Get(new IdentifiedContactReference(contactIdentifier.Source, contactIdentifier.Identifier), new ContactExecutionOptions(expandOptions));
                if (contact == null)
                {
                    model.ErrorList.Add("The contact can not be found based on the identifier information");
                    return;
                }

            }
            try
            {
                ExmMethods._clientApiService.SendAutomatedMessage(new AutomatedMessage
                {
                    ContactIdentifier = contactIdentifier,
                    MessageId = new Guid(model.MessageId),
                    CustomTokens = null,
                });
            }
            catch (Exception ex)
            {
                model.ErrorList.Add("The automated email was failed to send");
                ExmMethods._exmLogger.LogError(ex.Message, ex);
            }
        }

        public static void UpdateListSubscription(ListSubscribeOperation operation, string identifierValue, string identifierSource, string listId, string messageId, string managerRootId)
        {
            UpdateListSubscriptionMessage message = new UpdateListSubscriptionMessage()
                {
                    ContactIdentifier =
                        new ContactIdentifier(identifierSource, identifierValue, ContactIdentifierType.Known),
                    ListId = new Guid(listId),
                    ListSubscribeOperation = operation,
                    ManagerRootId = new Guid(managerRootId),
                    MessageId = new Guid(messageId),
                };
                _clientApiService.UpdateListSubscription(message);
        }

        private static byte[] ArgumentAsHexadecimalToByteArray(string value, string argumentName, bool acceptEmpty, ILogger logger)
        {
            Condition.Requires(argumentName, "argumentName").IsNotNull();
            Condition.Requires(logger, "logger").IsNotNull();
            if (!ByteArray.TryParseHexString(value, acceptEmpty, out var byteArray))
            {
                logger.LogError("Value must represent a valid hexadecimal value." + Environment.NewLine + "Parameter name: " + argumentName);
            }
            return byteArray;
        }

        
    }
}
