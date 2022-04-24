using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayZServerController
{
    internal class ModCopyHelper
    {
        public readonly int DayZGameID = 221100;

        private string _modSourceFolder;
        private string _modDestinationFolder;
        private Dictionary<long, string> _modMappingDict;

        // Stores the directories as keys and the Mod Server-Directory names as values
        private Dictionary<string, string> _observedModDirDict;
        private ModlistReader _modlistReader;
        private MultipleFileWatchers _modFileWatchers;
        private SteamApiWrapper _steamApiWrapper;
        private List<string> _modsToCopy;

        public IEnumerable<string> ModDestinationFolders
        {
            get
            {
                if(_observedModDirDict == null)
                    return Enumerable.Empty<string>();

                return _observedModDirDict.Values;
            }
        }

        public ModCopyHelper(string modFolder, string dayzServerExePath, ModlistReader modlistReader, SteamApiWrapper steamApiWrapper)
        {
            if (String.IsNullOrEmpty(modFolder) || !Directory.Exists(modFolder))
            {
               throw new ArgumentException($"Mod Source-Directory not found! ({modFolder ?? String.Empty})");
            }

            if (String.IsNullOrEmpty(dayzServerExePath) || !File.Exists(dayzServerExePath))
            {
                throw new ArgumentException($"DayZServer-Exe directory not found! ({dayzServerExePath ?? String.Empty})");
            }

            _modSourceFolder = modFolder;
            _modDestinationFolder = Path.GetDirectoryName(dayzServerExePath);
            _modlistReader = modlistReader;
            _modsToCopy = new List<string>();

            _steamApiWrapper = steamApiWrapper;
        }

        public async Task InitWatchingAsync()
        {
            Console.WriteLine("Fetching Mods from Modlist...");
            _modMappingDict = await _modlistReader.GetModsFromFile();
            Console.WriteLine($"{_modMappingDict.Count} Mods found in file.");

            _observedModDirDict = new Dictionary<string, string>();

            // Check for inconsistencies before the update

            IEnumerable<string> allModDirectories = Directory.GetDirectories(_modSourceFolder).Select(x => new DirectoryInfo(x).Name);

            // Check if all Mods from the Modlist are present inside the Mod Directory
            foreach (var modKeyValuePair in _modMappingDict)
            {
                // Is the mod missing?
                if (!allModDirectories.Contains(modKeyValuePair.Key.ToString()))
                {
                    Console.WriteLine($"WARNING: Mod {modKeyValuePair.Value} with ID {modKeyValuePair.Key} missing in Mod-Directory!");
                }
                else
                {
                    string pathToMod = Path.Combine(_modSourceFolder, modKeyValuePair.Key.ToString());
                    _observedModDirDict.Add(pathToMod, modKeyValuePair.Value);
                }
            }

            // Check for inconsistencies before an update via SteamAPI
            foreach (var modServerDirPair in _observedModDirDict)
            {
                string destinationPath = Path.Combine(_modDestinationFolder, modServerDirPair.Value);

                if (!MultipleFileWatchers.CheckIfDirectoryContentsAreEqual(modServerDirPair.Key, destinationPath))
                {
                    // Copy the content of the updated mod to the server mod folder
                    Console.WriteLine($"Mod {new DirectoryInfo(modServerDirPair.Value).Name} has a different file! Starting to copy to server mod folder.");

                    DirectoryCopyWorker copyWorker = new DirectoryCopyWorker(modServerDirPair.Key, destinationPath);
                    await copyWorker.CopyDirectory();
                }
            }

            _modFileWatchers = new MultipleFileWatchers(_observedModDirDict.Keys);
        }

        public async Task<int> CheckForUpdatesAsync()
        {
            _modsToCopy.Clear();

            // Start observering all necessary mod folders
            _modFileWatchers.StartWatching();

            foreach (var modKeyValuePair in _modMappingDict)
            {
                Console.WriteLine($"Checking for updates of Mod {modKeyValuePair.Value}...");
                int taskNo = await _steamApiWrapper.UpdateWorkshopItem(DayZGameID.ToString(), modKeyValuePair.Key.ToString());
                Console.WriteLine($"Closed SteamAPI-process {taskNo}.");
            }

            List<string> changedModList = _modFileWatchers.EndWatching();

            foreach(string modPath in changedModList)
            {
                if (!_modsToCopy.Contains(modPath))
                    _modsToCopy.Add(modPath);
            }

            Console.WriteLine($"{changedModList.Count} Mods have been updated by SteamAPI.");

            return changedModList.Count;
        }

        public async Task<int> UpdateModsAsync()
        {
            List<string> modsCopiedSuccessfully = new List<string>();

            foreach (string changedModDir in _modsToCopy)
            {
                string pathOfUpdatedMod = changedModDir;
                string modFolderNameInServerDir = _observedModDirDict[changedModDir];
                string serverModPath = Path.Combine(_modDestinationFolder, modFolderNameInServerDir);

                Console.WriteLine($"Starting to copy {modFolderNameInServerDir} Mod...");

                try
                {
                    DirectoryCopyWorker copyWorker = new DirectoryCopyWorker(pathOfUpdatedMod, serverModPath);
                    await copyWorker.CopyDirectory();
                    modsCopiedSuccessfully.Add(changedModDir);

                    if(_modsToCopy.Contains(pathOfUpdatedMod))
                        _modsToCopy.Remove(pathOfUpdatedMod);

                    Console.WriteLine($"Successfully copied {modFolderNameInServerDir} Mod to the server directory!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            // Remove the successfully copied mods from the ToDo-List
            modsCopiedSuccessfully.ForEach(x => _modsToCopy.Remove(x));
            
            return modsCopiedSuccessfully.Count;
        }
    }
}
