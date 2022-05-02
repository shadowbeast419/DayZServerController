using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayZServerController
{
    internal class ModManager
    {
        public readonly int DayZGameID = 221100;

        private DirectoryInfo _workshopModFolder;
        private DirectoryInfo _dayzServerFolder;
        private Dictionary<long, string> _modListDict;

        // Stores the Workshop Mod Directories as keys and the DayZ-Server Mod Directories as values
        private Dictionary<DirectoryInfo, DirectoryInfo> _workshopServerModFolderDir;
        private ModlistReader _modlistReader;
        private MultipleFileWatchers _modFileWatchers;
        private SteamApiWrapper _steamApiWrapper;

        /// <summary>
        /// Names of all Mod Folders in DayZ-Server Directory (from Modlist)
        /// </summary>
        public IEnumerable<string> ServerFolderModDirectoryNames
        {
            get
            {
                if(_workshopServerModFolderDir == null)
                    return Enumerable.Empty<string>();

                return _workshopServerModFolderDir.Values.Select(x => x.Name);
            }
        }

        public ModManager(DirectoryInfo workshopModFolder, FileInfo dayzServerExeInfo, ModlistReader modlistReader, SteamApiWrapper steamApiWrapper)
        {
            if (!workshopModFolder.Exists)
            {
               throw new ArgumentException($"Mod Source-Directory not found! ({workshopModFolder.FullName})");
            }

            if (!dayzServerExeInfo.Exists)
            {
                throw new ArgumentException($"DayZServer-Exe directory not found! ({dayzServerExeInfo.FullName})");
            }

            _workshopModFolder = workshopModFolder;
            _dayzServerFolder = new DirectoryInfo(dayzServerExeInfo.DirectoryName);
            _modlistReader = modlistReader;
            _workshopServerModFolderDir = new Dictionary<DirectoryInfo, DirectoryInfo>();
            _steamApiWrapper = steamApiWrapper;
        }

        /// <summary>
        /// Updates the mods from the Modlist
        /// </summary>
        /// <returns></returns>
        public async Task UpdateModDirsFromModlistAsync()
        {
            Console.WriteLine("Fetching Mods from Modlist...");
            _modListDict = await _modlistReader.GetModsFromFile();
            Console.WriteLine($"{_modListDict.Count} Mods found in file.");

            _workshopServerModFolderDir.Clear();

            // Get all ModFolders from the WorkshopFolder (with IDs as names)
            IEnumerable<string> allModDirectories = _workshopModFolder.GetDirectories().Select(x => x.Name);

            // Check if all Mods from the Modlist are present inside the Mod Directory
            foreach (var modKeyValuePair in _modListDict)
            {
                // Is the mod missing?
                if (!allModDirectories.Contains(modKeyValuePair.Key.ToString()))
                {
                    Console.WriteLine($"WARNING: Mod {modKeyValuePair.Value} with ID {modKeyValuePair.Key} missing in Mod-Directory!");
                }
                else
                {
                    string pathToWorkshopModFolder = Path.Combine(_workshopModFolder.FullName, modKeyValuePair.Key.ToString());
                    string pathToServerModFolder = Path.Combine(_dayzServerFolder.FullName, modKeyValuePair.Value);

                    _workshopServerModFolderDir.Add(new DirectoryInfo(pathToWorkshopModFolder), new DirectoryInfo(pathToServerModFolder));
                }
            }
        }

        /// <summary>
        /// Function calls SteamCMD which checks for updates of all mods, FileWatchers detect changes
        /// </summary>
        /// <returns></returns>
        public async Task<int> DownloadModUpdatesViaSteamAsync()
        {
            // Watch the Workshop Mod Directories for changes
            _modFileWatchers = new MultipleFileWatchers(_workshopServerModFolderDir.Keys);

            // Start observering all necessary mod folders
            _modFileWatchers.StartWatching();
            _steamApiWrapper.ResetUpdateTaskList();

            foreach (var modKeyValuePair in _modListDict)
            {
                Console.WriteLine($"Adding task for checking for updates of Mod {modKeyValuePair.Value}...");
                 _steamApiWrapper.AddUpdateWorkshopItemTask(DayZGameID.ToString(), modKeyValuePair.Key.ToString());
            }

            Console.WriteLine($"Executing steamCMD-process with {_steamApiWrapper.ModUpdateTasksCount} ModUpdate-Tasks...");
            int taskNo = await _steamApiWrapper.ExecuteSteamCMDWithArguments();
            Console.WriteLine($"Closed SteamAPI-process {taskNo}.");

            IList<DirectoryInfo> changedModList = _modFileWatchers.EndWatching();
            List<DirectoryInfo> _modsToCopy = new List<DirectoryInfo>();

            foreach(DirectoryInfo modPath in changedModList)
            {
                if (!_modsToCopy.Contains(modPath))
                    _modsToCopy.Add(modPath);
            }

            Console.WriteLine($"{changedModList.Count} Mods have been updated by SteamAPI.");

            return changedModList.Count;
        }

        /// <summary>
        /// Syncs Mod Folder contents locally. 
        /// </summary>
        /// <param name="sourceDestFolderMap"></param>
        /// <returns></returns>
        public async Task SyncWorkshopWithServerModsAsync()
        {
            foreach (var sourceDestTuple in _workshopServerModFolderDir)
            {
                if (!MultipleFileWatchers.CheckIfDirectoryContentsAreEqual(sourceDestTuple.Key, sourceDestTuple.Value))
                {
                    // Copy the content of the updated mod to the server mod folder
                    Console.WriteLine($"Mod {sourceDestTuple.Value.Name} has a different file! " +
                                      $"Starting to copy to server mod folder.");

                    using (var copyWorker = new DirectoryCopyWorker(sourceDestTuple.Key, sourceDestTuple.Value))
                    {
                        await copyWorker.CopyDirectory();
                    }
                }
            }
        }
    }
}
