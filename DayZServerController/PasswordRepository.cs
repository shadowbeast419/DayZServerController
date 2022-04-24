using CredentialManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayZServerController
{
    internal static class PasswordRepository
    {
        public static bool SavePassword(string credentialName, string username, string password)
        {
            using(var cred = new Credential())
            {
                cred.Username = username;
                cred.Password = password;
                cred.Target = credentialName;
                cred.Type = CredentialType.Generic;
                cred.PersistanceType = PersistanceType.LocalComputer;
                return cred.Save();
            }
        }

        public static Credential GetPassword(string credentialName)
        {
            Credential cred = new Credential();
            cred.Target = credentialName;

            if (!cred.Load())
            {
                return null;
            }

            return cred;
        }
    }
}
