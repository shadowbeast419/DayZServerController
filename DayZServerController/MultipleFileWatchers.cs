using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayZServerController
{
    public class MultipleFileWatchers
    {
        private IEnumerable<DirectoryInfo> _directoriesToWatch;
        private IList<DirectoryInfo> _changedDirectories;
        private Dictionary<DirectoryInfo, List<FileInfo>> _fileInfoDictStart;
        private Dictionary<DirectoryInfo, List<FileInfo>> _fileInfoDictEnd;

        public MultipleFileWatchers(IEnumerable<DirectoryInfo> directoriesToWatch)
        {
            _directoriesToWatch = directoriesToWatch.Where(x => x.Exists);
            _changedDirectories = new List<DirectoryInfo>();

            _fileInfoDictStart = new Dictionary<DirectoryInfo, List<FileInfo>>();
            _fileInfoDictEnd = new Dictionary<DirectoryInfo, List<FileInfo>>();
        }

        public void StartWatching()
        {
            _fileInfoDictStart.Clear();

            foreach (DirectoryInfo dirToWatch in _directoriesToWatch)
            {
                _fileInfoDictStart.Add(dirToWatch, GetFileInfoFromDirectory(dirToWatch));
            }

            Console.WriteLine($"FileWatchers: Observing {_directoriesToWatch.Count()} directories.");
        }

        private static List<FileInfo> GetFileInfoFromDirectory(DirectoryInfo directory)
        {
            List<FileInfo> fileInfos = new List<FileInfo>();

            // Watch the mod directory for updates
            foreach (string file in Directory.EnumerateFiles(directory.FullName, "*.*", SearchOption.AllDirectories))
            {
                // Create the FileInfo object only when needed to ensure
                // the information is as current as possible.
                FileInfo fi = null;

                try
                {
                    fi = new FileInfo(file);
                    fileInfos.Add(fi);
                }
                catch (FileNotFoundException e)
                {
                    // To inform the user and continue is
                    // sufficient for this demonstration.
                    // Your application may require different behavior.
                    Console.WriteLine(e.Message);
                    continue;
                }
            }

            return fileInfos;
        }

        private static bool CheckIfFileInfosAreEqual(IList<FileInfo> startDir, IList<FileInfo> endDir)
        {
            // Check if the counts fit
            if (startDir.Count != endDir.Count)
            {
                Console.WriteLine($"Different count of files for mod detected. " +
                    $"(Before: {startDir.Count}, After: {endDir.Count})");

                return false;
            }

            foreach (FileInfo fileInfoStart in startDir)
            {
                // Check if a file has been deleted (File is in FileInfoStart and not in FileInfoEnd)
                if (endDir.FirstOrDefault(x => x.Name == fileInfoStart.Name) == null)
                {
                    Console.WriteLine($"Deleted file detected. {fileInfoStart.Name}");

                    return false;
                }

                // Compare each start file with every end file
                foreach (FileInfo fileInfoEnd in endDir)
                {
                    string parentDirStart = new DirectoryInfo(fileInfoStart.FullName).Parent.Name;
                    string parentDirEnd = new DirectoryInfo(fileInfoEnd.FullName).Parent.Name;

                    // Has the filesize changed? (Take into account that there can mulitple files with the same name in different dirs)
                    if (fileInfoStart.Name == fileInfoEnd.Name && parentDirStart == parentDirEnd && fileInfoStart.Length != fileInfoEnd.Length)
                    {
                        Console.WriteLine($"Change in filesize of file {fileInfoStart.Name} detected!");

                        return false;
                    }

                    // Check if a file has been added (File is in FileInfoEnd and not in FileInfoStart)
                    if (startDir.FirstOrDefault(x => x.Name == fileInfoEnd.Name) == null)
                    {
                        Console.WriteLine($"New file detected. {fileInfoEnd.Name}");

                        return false;
                    }
                }
            }

            // File in this directory has not changed
            return true;
        }

        public IList<DirectoryInfo> EndWatching()
        {
            _fileInfoDictEnd.Clear();
            _changedDirectories.Clear();

            foreach (DirectoryInfo dirToWatch in _directoriesToWatch)
            {
                _fileInfoDictEnd.Add(dirToWatch, GetFileInfoFromDirectory(dirToWatch));
            }

            // Compare the FileInfo from the start with the Info from the end
            foreach(DirectoryInfo dirToWatch in _directoriesToWatch)
            {
                if(!CheckIfFileInfosAreEqual(_fileInfoDictStart[dirToWatch], _fileInfoDictEnd[dirToWatch]))
                {
                    Console.WriteLine($"Directory {dirToWatch} changed!");
                    _changedDirectories.Add(dirToWatch);
                }
            }

            Console.WriteLine($"FileWatcher: Ended watching {_directoriesToWatch.Count()} directories.");
            Console.WriteLine($"Found {_changedDirectories.Count} Mods for update!");

            foreach (DirectoryInfo changedDir in _changedDirectories)
            {
                Console.WriteLine(changedDir.Name);
            }

            return _changedDirectories;
        }

        public static bool CheckIfDirectoryContentsAreEqual(DirectoryInfo dir1, DirectoryInfo dir2)
        {
            return CheckIfFileInfosAreEqual(GetFileInfoFromDirectory(dir1), GetFileInfoFromDirectory(dir2));
        }
    }
}
