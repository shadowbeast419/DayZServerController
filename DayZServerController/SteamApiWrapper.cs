using CredentialManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayZServerController
{
    internal class SteamApiWrapper
    {

        public readonly int DayZGameID = 221100;

        private string _steamApiPath;
        private string _credentialsName = "SteamCredentials2";
        private bool _isInitialized = false;
        private Credential _steamCredentials;

        public SteamApiWrapper(string steamApiPath)
        {
            if (String.IsNullOrEmpty(steamApiPath))
                throw new ArgumentNullException($"SteamApi-Path is null or empty!");

            if (!File.Exists(steamApiPath))
                throw new ArgumentException($"SteamApi not found {steamApiPath}!");

            _steamApiPath = steamApiPath;
        }

        public bool Init(string username = null, string password = null)
        {
            if(String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password))
            {
                // Look for an existing password
                Credential credentials = PasswordRepository.GetPassword(_credentialsName);

                if (credentials == null)
                    return false;

                _steamCredentials = credentials;
            }
            else
            {
                // Store a new password
                if (!PasswordRepository.SavePassword(_credentialsName, username, password))
                    return false;

                _steamCredentials = PasswordRepository.GetPassword(_credentialsName);
            }

            _isInitialized = true;

            return true;
        }

        public Task<int> UpdateWorkshopItem(string gameID, string modID)
        {
            List<string> cliArguments = new List<string>();
            cliArguments.Add("+login");
            cliArguments.Add(_steamCredentials.Username);
            cliArguments.Add(_steamCredentials.Password);
            cliArguments.Add($"+set_steam_guard_code 68F5M");
            cliArguments.Add("\"+workshop_download_item");
            cliArguments.Add(gameID);
            cliArguments.Add($"{modID}\"");
            cliArguments.Add("validate");
            cliArguments.Add("+quit");

            return ProcessHelper.Start(_steamApiPath, cliArguments);
        }
    }
}
