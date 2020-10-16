using Microsoft.Azure.KeyVault.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Profile
{
    public class CustomKeyVaultSecretManager : IKeyVaultSecretManager
    {
        private readonly string _prefix;

        public CustomKeyVaultSecretManager(string prefix)
        {
            this._prefix = prefix;
        }

        public string GetKey(SecretBundle secret)
        {
            return secret.SecretIdentifier.Name.Substring(_prefix.Length).Replace("--",
                ConfigurationPath.KeyDelimiter);
        }

        public bool Load(SecretItem secret)
        {
            return secret.Identifier.Name.StartsWith(this._prefix);
        }
    }
}
