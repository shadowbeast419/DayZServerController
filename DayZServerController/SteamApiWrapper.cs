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
        public static readonly string DayZServerGameID = "223350";

        private FileInfo _steamApiPath;
        private string _credentialsName = "SteamCredentials2";
        private bool _isInitialized = false;
        private Credential _steamCredentials;

        private List<string> _cliArguments = new List<string>();
        private readonly string _steamGuardCode = "PH8MY";
        private List<string> _defaultCliStartArguments = new List<string>();
        private List<string> _defaultCliEndArguments = new List<string>();

        public int ModUpdateTasksCount { get; private set; } = 0;

        public SteamApiWrapper(FileInfo steamApiPath)
        {
            if (!steamApiPath.Exists)
                throw new ArgumentNullException($"SteamApi-Path is not valid!");

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

            _defaultCliStartArguments.Clear();

            // Add the default login command to the cli arguments
            _defaultCliStartArguments.Add($"+login");
            _defaultCliStartArguments.Add(_steamCredentials.Username);
            _defaultCliStartArguments.Add(_steamCredentials.Password);
            // _defaultCliStartArguments.Add($"+set_steam_guard_code {_steamGuardCode}");

            _defaultCliEndArguments.Add("+quit");

            _isInitialized = true;

            return true;
        }

        public async Task UpdateDayZServer()
        {
            ResetUpdateTaskList();
            AddUpdateGameTask(DayZServerGameID);

            await ExecuteSteamCMDWithArguments();
        }

        private void AddUpdateGameTask(string gameID)
        {
            if (!_isInitialized)
                return;

            if (String.IsNullOrEmpty(gameID))
                throw new ArgumentException("SteamApiWrapper: GameID is invalid.");

            _cliArguments.Add($"\"+app_update");
            _cliArguments.Add($"{gameID}\"");
        }

        public void AddUpdateWorkshopItemTask(string gameID, string modID)
        {
            if (!_isInitialized)
                return;

            if (String.IsNullOrEmpty(gameID) || string.IsNullOrEmpty(modID))
                throw new ArgumentException("SteamApiWrapper: GameID or ModID is invalid.");

            _cliArguments.Add("\"+workshop_download_item");
            _cliArguments.Add(gameID);
            _cliArguments.Add($"{modID}\"");

            ModUpdateTasksCount++;
        }

        public Task<int> ExecuteSteamCMDWithArguments()
        {
            // Return dummy task if SteamCMD hasn't been initialized
            if (!_isInitialized)
                return new Task<int>(() => { return 0; });

            // Insert first and last arguments which are always the same
            _cliArguments.InsertRange(0, _defaultCliStartArguments);
            _cliArguments.InsertRange(_cliArguments.Count, _defaultCliEndArguments);

            return ProcessHelper.Start(_steamApiPath.FullName, _cliArguments);
        }

        public void ResetUpdateTaskList()
        {
            _cliArguments.Clear();
            ModUpdateTasksCount = 0;
        }
    }
}
