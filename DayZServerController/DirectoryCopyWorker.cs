using RoboSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayZServerController
{
    internal class DirectoryCopyWorker
    {
        private string _sourceDir;
        private string _destinationDir;
        private bool _copyingFinished = false;

        public bool DirectoriesValid { get; } = false;


        public DirectoryCopyWorker(string source, string destination)
        {
            if (String.IsNullOrEmpty(source) || !Directory.Exists(source))
                throw new DirectoryNotFoundException($"Robocopy: Could not find source directory {source ?? "null"}!");

            if (String.IsNullOrEmpty(destination) || !Directory.Exists(destination))
                throw new DirectoryNotFoundException($"Robocopy: Could not find destination directory {destination ?? "null"}");

            _sourceDir = source;
            _destinationDir = destination;

            DirectoriesValid = true;
        }

        public async Task CopyDirectory()
        {
            if (!DirectoriesValid)
            {
                Console.WriteLine($"Robocopy: WARNING: Directories invalid, no copying done.");
                return;
            }

            RoboCommand roboCmd = new RoboCommand();

            // events
            roboCmd.OnCommandCompleted += RoboCmd_OnCommandCompleted;

            // copy options
            roboCmd.CopyOptions.Source = _sourceDir;
            roboCmd.CopyOptions.Destination = _destinationDir;
            roboCmd.CopyOptions.UseUnbufferedIo = true;
            roboCmd.CopyOptions.Mirror = true;

            // retry options
            roboCmd.RetryOptions.RetryCount = 1;
            roboCmd.RetryOptions.RetryWaitTime = 2;

            Console.WriteLine($"Robocopy: Copying content of directory {_sourceDir} to {_destinationDir}...");
            await roboCmd.Start();
            
            while(!_copyingFinished)
            {
                await Task.Delay(500);
            }

            Console.WriteLine($"Robocopy: Leaving...");
            _copyingFinished = false;
        }

        private void RoboCmd_OnCommandCompleted(object sender, RoboCommandCompletedEventArgs e)
        {
            Console.WriteLine($"Robocopy: Copying directory complete! (Copied files: {e.Results.FilesStatistic.Copied}");
            _copyingFinished = true;
        }
    }
}
