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

namespace EXMExtension.Tools
{
    static class ExmMethods
    {
        private static ILogger _exmLogger = new Logger("Sitecore.EXM", "false");

        private static string _cryptoKeyName = "EXM.CryptographicKey";

        private static string _authKeyName = "EXM.AuthenticationKey";

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
                query = HttpUtility.HtmlDecode(query);
                result = cipher.TryDecrypt(query);
                return result; // decryption fail
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
