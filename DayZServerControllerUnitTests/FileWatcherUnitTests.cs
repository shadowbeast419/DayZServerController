using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using DayZServerController;

namespace DayZServerControllerUnitTests
{
    [TestFixture]
    public class FileWatcherUnitTests
    {
        private MultipleFileWatchers _fileWatchers;
        private static Random _random = new Random();
        private string _baseTestDirectory = String.Empty;
        private List<string> _createdRandomDirs;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _baseTestDirectory = Directory.GetCurrentDirectory();
            _createdRandomDirs = new List<string>();
        }

        [Test]
        public void CheckIfChangesGetNoticed()
        {
            List<string> directoriesToWatch = CreateDirectories(_baseTestDirectory).ToList();
            _createdRandomDirs.AddRange(directoriesToWatch);

            _fileWatchers = new MultipleFileWatchers(directoriesToWatch);
            _fileWatchers.StartWatching();

            for(int i = 0; i < directoriesToWatch.Count; i++)
            {
                // Add files to every third directory
                if(i % 3 == 0)
                    AddRandomFilesToDirectory(directoriesToWatch[i]);
            }

            List<string> changedDirectories = _fileWatchers.EndWatching();

            for (int i = 0; i < directoriesToWatch.Count; i++)
            {
                // Check if every third directory has changed according to the FileWatchers
                if(i % 3 == 0)
                {
                    Assert.IsTrue(changedDirectories.Contains(directoriesToWatch[i]));
                }
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            foreach (string dir in _createdRandomDirs)
            {
                // Clean up the randomly ceated directories and files
                RecursiveDelete(new DirectoryInfo(dir));
            }
        }

        private static IEnumerable<string> AddRandomFilesToDirectory(string dir, int count = 5)
        {
            List<string> fileList = new List<string>();

            for(int i = 0; i < count; i++)
            {
                string filePath = Path.Combine(dir, $"{RandomString(10)}.{RandomString(3)}");

                FileStream fileStream = File.Create(filePath);
                fileStream.Close();

                fileList.Add(filePath);
            }

            return fileList;
        }

        private static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// Creates 10 random directories for the FileWatchers to watch
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        private IEnumerable<string> CreateDirectories(string baseDir, int count = 10)
        {
            List<string> directories = new List<string>();

            for (int i = 0; i < 10; i++)
            {
                directories.Add(Path.Combine(baseDir, $"{RandomString(5)}\\"));
                Directory.CreateDirectory(directories.Last());
            }

            return directories;
        }

        private static void RecursiveDelete(DirectoryInfo baseDir)
        {
            if (!baseDir.Exists)
                return;

            foreach (var dir in baseDir.EnumerateDirectories())
            {
                RecursiveDelete(dir);
            }

            baseDir.Delete(true);
        }
    }
}
