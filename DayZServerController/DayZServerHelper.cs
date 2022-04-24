using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayZServerController
{
    internal class DayZServerHelper
    {
        private string _dayZExecPath = String.Empty;
        private string _dayZServerProcName = String.Empty;
        private int _restartInterval;
        private System.Timers.Timer _restartTimer;
        private DateTime? _startTime;
        private bool _timerStoppedManually = false;

        public int ServerPort { get; set; } = 2302;
        public string ServerConfig { get; set; } = "serverDZ.cfg";
        public int ServerCPUCount { get; set; } = 2;
        public string ProfileFolderName { get; set; } = "Profiles";

        public bool IsRunning
        {
            get
            {
                return ProcessHelper.IsRunning(_dayZServerProcName);
            }
        }

        public TimeSpan? TimeUntilNextRestart
        {
            get
            {
                DateTime? timeOfNextRestart = TimeOfNextRestart;

                if (!timeOfNextRestart.HasValue)
                    return null;

                return TimeOfNextRestart.Value - DateTime.Now;
            }
        }

        public DateTime? TimeOfNextRestart
        {
            get
            {
                if (_restartTimer == null || _restartTimer.Enabled == false || !_startTime.HasValue)
                    return null;

               return _startTime.Value + TimeSpan.FromMilliseconds(_restartInterval);
            }
        }

        public event Action RestartTimerElapsed; 

        public DayZServerHelper(string pathToDayZExec, string restartIntervalString)
        {
            if (String.IsNullOrEmpty(pathToDayZExec) ||
                    !File.Exists(pathToDayZExec))
            {
                throw new ArgumentException($"No valid name for DayZServer-Executable or path not found! ({pathToDayZExec ?? String.Empty})");
            }

            _dayZExecPath = pathToDayZExec;
            _dayZServerProcName = Path.GetFileNameWithoutExtension(pathToDayZExec);

            if (!int.TryParse(restartIntervalString, out int restartInterval) || restartInterval <= 0)
            {
                throw new ArgumentException($"Invalid Restart-Interval! ({restartIntervalString ?? String.Empty})");
            }

            _restartInterval = restartInterval * 1000;

            _restartTimer = new System.Timers.Timer();
            _restartTimer.Elapsed += RestartTimer_Elapsed;
            _restartTimer.Interval = _restartInterval;
            _restartTimer.AutoReset = true;
        }

        public void StartServer(IEnumerable<string> modsToEnable)
        {
            List<string> cliArguments = new List<string>();

            cliArguments.Add($"-config={ServerConfig}");
            cliArguments.Add($"-port={ServerPort}");
            cliArguments.Add("-dologs");
            cliArguments.Add("-adminlog");
            cliArguments.Add("-netlog");
            cliArguments.Add("-freezecheck");

            List<string> modsAlreadyAdded = new List<string>();

            // Prioritize the mods where the sorting is relevant
            if (modsToEnable.Contains("@CF"))
                modsAlreadyAdded.Add("@CF");

            if(modsToEnable.Contains("@Dabs-Framework"))
                modsAlreadyAdded.Add("@Dabs-Framework");

            if (modsToEnable.Contains("@Community-Online-Tools"))
                modsAlreadyAdded.Add("@Community-Online-Tools");

            if (modsToEnable.Contains("@DayZ-Expansion-Licensed"))
                modsAlreadyAdded.Add("@DayZ-Expansion-Licensed");

            if (modsToEnable.Contains("@DayZ-Expansion-Core"))
                modsAlreadyAdded.Add("@DayZ-Expansion-Core");

            if (modsToEnable.Contains("@DayZ-Expansion"))
                modsAlreadyAdded.Add("@DayZ-Expansion");

            if (modsToEnable.Contains("@DayZ-Expansion-Book"))
                modsAlreadyAdded.Add("@DayZ-Expansion-Book");

            if (modsToEnable.Contains("@DayZ-Expansion-Market"))
                modsAlreadyAdded.Add("@DayZ-Expansion-Market");

            if (modsToEnable.Contains("@DayZ-Expansion-Vehicles"))
                modsAlreadyAdded.Add("@DayZ-Expansion-Vehicles");

            // Add the rest
            foreach(string modToEnable in modsToEnable)
            {
                if (!modsAlreadyAdded.Contains(modToEnable))
                    modsAlreadyAdded.Add(modToEnable);
            }

            StringBuilder modStringBuilder = new StringBuilder();

            foreach(string modToAdd in modsAlreadyAdded)
            {
                modStringBuilder.Append(modToAdd + ";");
            }

            cliArguments.Add($"\"-mod={modStringBuilder}\"");
            cliArguments.Add($"\"-profiles={ProfileFolderName}\"");

            Console.WriteLine($"Starting DayZServer with generated CLI-Arguments.");
            Console.WriteLine($"Arguments: {String.Join(' ', cliArguments)}");

            ProcessHelper.Start(_dayZExecPath, cliArguments);
        }

        public void StartRestartTimer()
        {
            _restartTimer.Start();
            _startTime = DateTime.Now;
        }

        public void StopServer()
        {
            int killedProcs = ProcessHelper.Kill(_dayZServerProcName);
            Console.WriteLine($"Killed {killedProcs} DayZServer Processes");
        }

        public void StopRestartTimer()
        {
            _timerStoppedManually = true;
            _restartTimer.Stop();
            _timerStoppedManually = false;
        }

        private void RestartTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if(!_timerStoppedManually)
                RestartTimerElapsed?.Invoke();
        }
    }
}
