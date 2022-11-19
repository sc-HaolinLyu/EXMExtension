using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Diagnostics;

namespace EXMExtension.Models
{
    public class DecryptEmailLinkModel:BaseTaskWrapper
    {
        public static string CryptoKey;

        public static string AuthKey;

        public string OverrideCryptoKey;

        public string OverrideAuthKey;

        public string DecryptionResult;

        public string QueryString;



        public DecryptEmailLinkModel()
        {
            ErrorList = new List<string>();
        }
        public override void Reset()
        {
            OverrideAuthKey = string.Empty;

            OverrideCryptoKey = string.Empty;

            DecryptionResult = string.Empty;

            ErrorList.Clear();
        }
    }
}
