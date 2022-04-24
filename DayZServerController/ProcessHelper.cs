using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayZServerController
{
    public static class ProcessHelper
    {
        public static bool IsRunning(string name) => Process.GetProcessesByName(name).Length > 0;

        public static int Kill(string name)
        {
            Process[] processes = Process.GetProcessesByName(name);

            foreach(Process process in processes)
            {
                if (!process.CloseMainWindow())
                    process.Kill();
            }

            return processes.Length;
        }

        public static Task<int> Start(string executablePath, IEnumerable<string> cliArguments)
        {
            StringBuilder sb = new StringBuilder();

            foreach(string cliArgument in cliArguments)
            {
                sb.Append(cliArgument);
                sb.Append(" ");
            }

            string cliArgumentString = sb.ToString().TrimEnd(' ');

            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

            Process process = new Process
            {
                StartInfo = { FileName = executablePath, Arguments = cliArgumentString },
                EnableRaisingEvents = true
            };

            process.Exited += (sender, args) =>
            {
                tcs.SetResult(process.ExitCode);
                process.Dispose();
            };

            process.Start();

            // Enable CPU Affinity for 8 Cores
            foreach(ProcessThread procThread in process.Threads)
            {
                procThread.ProcessorAffinity = (IntPtr)0x007F;
            }

            return tcs.Task;
        }
    }
}
