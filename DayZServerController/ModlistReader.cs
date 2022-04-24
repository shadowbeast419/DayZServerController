using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayZServerController
{
    internal class ModlistReader
    {
        private string _modlistPath;

        public ModlistReader(string modlistPath)
        {
            if (String.IsNullOrEmpty(modlistPath))
                throw new ArgumentNullException($"Modlist-Path is null or empty!");

            if (!File.Exists(modlistPath))
                throw new ArgumentException($"Modlist not found {modlistPath}!");

            _modlistPath = modlistPath;
        }

        public async Task<Dictionary<long, string>> GetModsFromFile()
        {
            Dictionary<long, string> modDict = new Dictionary<long, string>();

            using(StreamReader sr = new StreamReader(_modlistPath))
            {
                while(!sr.EndOfStream)
                {
                    string? modListLine = await sr.ReadLineAsync();

                    if (String.IsNullOrEmpty(modListLine))
                        continue;

                    string[] splittedModListLine = modListLine.Split(',');

                    if (splittedModListLine.Length != 2 || 
                        String.IsNullOrEmpty(splittedModListLine[0]) || String.IsNullOrEmpty(splittedModListLine[1]))
                        continue;

                    if (!long.TryParse(splittedModListLine[0], out long workshopID))
                        continue;

                    modDict.Add(workshopID, splittedModListLine[1]);
                }
            }

            return modDict;
        }
    }
}
