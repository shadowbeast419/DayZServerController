using RoboSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayZServerController
{
    internal class DirectoryCopyWorker : IDisposable
    {
        private DirectoryInfo _sourceDir;
        private DirectoryInfo _destinationDir;
        private bool _copyingFinished = false;
        private RoboCommand _roboCmd;

        public DirectoryCopyWorker(DirectoryInfo source, DirectoryInfo destination)
        {
            if (!source.Exists)
                throw new DirectoryNotFoundException($"Robocopy: Could not find source directory {source.FullName}!");

            if (!destination.Exists)
                throw new DirectoryNotFoundException($"Robocopy: Could not find destination directory {destination.FullName}");

            _sourceDir = source;
            _destinationDir = destination;
            _roboCmd = new RoboCommand();
        }

        public async Task CopyDirectory()
        {
            if (!_sourceDir.Exists || !_destinationDir.Exists)
            {
                Console.WriteLine($"Robocopy: WARNING: Directories invalid, no copying done.");
                return;
            }

            // events
            _roboCmd.OnCommandCompleted += RoboCmd_OnCommandCompleted;

            // copy options
            _roboCmd.CopyOptions.Source = _sourceDir.FullName;
            _roboCmd.CopyOptions.Destination = _destinationDir.FullName;
            _roboCmd.CopyOptions.UseUnbufferedIo = true;
            _roboCmd.CopyOptions.Mirror = true;

            // retry options
            _roboCmd.RetryOptions.RetryCount = 1;
            _roboCmd.RetryOptions.RetryWaitTime = 2;

            Console.WriteLine($"Robocopy: Copying content of directory {_sourceDir} to {_destinationDir}...");
            await _roboCmd.Start();
            
            while(!_copyingFinished)
            {
                await Task.Delay(500);
            }

            Console.WriteLine($"Robocopy: Leaving...");
            _copyingFinished = false;
        }

        private void RoboCmd_OnCommandCompleted(object sender, RoboCommandCompletedEventArgs e)
        {
            if (!e.Results.Status.Successful)
            {
                Console.WriteLine($"Robocopy: Error copying files. ({String.Join('\n', e.Results.LogLines)})");
            }

            Console.WriteLine($"Robocopy: Copying directory complete! (Copied files: {e.Results.FilesStatistic.Copied}");
            _copyingFinished = true;
        }

        public void Dispose()
        {
            _roboCmd.OnCommandCompleted -= RoboCmd_OnCommandCompleted;
            _roboCmd?.Dispose();
        }
    }
}
