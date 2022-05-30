
using DayZServerController;
using System.Diagnostics;

// -------------- Checking CLI Arguments ---------------------------------------
bool muteDiscordBot = false;

// If set to false, a native Steam Client has to do the updates
bool useSteamCmdForUpdates = false;

if (args.Length != Enum.GetNames(typeof(CliArgumentIndices)).Length)
{
    Console.WriteLine($"Invalid number of CLI-Arguments. Is: {args.Length}, " +
        $"Should: {Enum.GetNames(typeof(CliArgumentIndices)).Length}. Exiting application...");
    Console.ReadLine();

    return 1;
}

SteamCmdWrapper steamApiWrapper;
ModManager modManager;
DayZServerHelper dayZServerHelper;
DiscordBot discordBot =
    new DiscordBot(new DiscordBotData(new FileInfo(args[(int)CliArgumentIndices.DiscordFilePath])))
{
    Mute = muteDiscordBot
};

Logging logger = new Logging(discordBot)
{
    MuteDiscordBot = muteDiscordBot
};


try
{
    steamApiWrapper = useSteamCmdForUpdates ? 
        new SteamCmdWrapper(SteamCmdModeEnum.SteamCmdExe,new FileInfo(args[(int)CliArgumentIndices.SteamCmdPath])) : 
        new SteamCmdWrapper();

    modManager = new ModManager(new DirectoryInfo(args[(int)CliArgumentIndices.WorkshopFolder]),
        new FileInfo(args[(int)CliArgumentIndices.DayZServerExecPath]),
        new ModlistReader(new FileInfo(args[(int)CliArgumentIndices.ModlistPath])),
        steamApiWrapper);

    dayZServerHelper = new DayZServerHelper(args[(int)CliArgumentIndices.DayZServerExecPath],
        args[(int)CliArgumentIndices.RestartPeriod]);
}
catch (ArgumentException ex)
{
    Console.WriteLine(ex.Message);
    Console.ReadLine();
    return 1;
}

// -------------- Steam Credentials ---------------------------------------

// First try to retrieve existing steam credentials
if (!steamApiWrapper.Init())
{
    // No credentials stored
    Console.WriteLine("No steam credentials found. Please enter them now:");
    Console.Write("Username: ");

    string? steamUsername = Console.ReadLine();
    Console.WriteLine();
    Console.Write("Password: ");

    string? steamPassword = Console.ReadLine();

    if(String.IsNullOrEmpty(steamUsername) || String.IsNullOrEmpty(steamPassword))
    {
        Console.WriteLine("Credentials are no valid strings. Exiting application...");
        Console.ReadLine();
        return 1;
    }

    // Init with new credentials
    if(!steamApiWrapper.Init(steamUsername, steamPassword))
    {
        Console.WriteLine("Could not store new steam credentials. Exiting application...");
        Console.ReadLine();
        return 1;
    }
}

await discordBot.Init();

if (dayZServerHelper.IsRunning)
{
    Console.Write("Server is already running: Do you want to restart the process? (y/n)");
    
    ConsoleKeyInfo pressedKey = Console.ReadKey();
    bool stopServer = pressedKey.Key == ConsoleKey.Y;

    if(stopServer)
    {
        // Server is already running at startup of this exec -> stop it and restart with fitting restart interval
        dayZServerHelper.StopServer();
        await Task.Delay(TimeSpan.FromSeconds(20));
    }
    else
    {
        Console.WriteLine("Application will now exit...");
        Console.ReadLine();
        return 1;
    }
}

// Get data from the Modlist file
await modManager.UpdateModDirsFromModlistAsync();

// Update DayZ Server Exec via SteamCMD (if update is available)
// await logger.WriteLineAsync($"Checking for DayZServer Updates...");
// await steamApiWrapper.UpdateDayZServer();

int modsUpdated = await modManager.DownloadModUpdatesViaSteamAsync();

if (modsUpdated > 0 || modManager.ModUpdateAvailable)
{
    await logger.WriteLineAsync($"Updated Mod(s) found!", false);

    int syncedModsLocal = await modManager.SyncWorkshopWithServerModsAsync();
    await logger.WriteLineAsync($"Synced {syncedModsLocal} Mod(s) locally");
}

// Start restart timer and server
dayZServerHelper.StartServer(modManager.ServerFolderModDirectoryNames);

dayZServerHelper.RestartTimerElapsed += DayZServerHelperRestartTimerElapsed;
dayZServerHelper.StartRestartTimer();

await logger.WriteLineAsync($"Server started! Next restart scheduled at " +
                            $"{(dayZServerHelper.TimeOfNextRestart.HasValue ? dayZServerHelper.TimeOfNextRestart.Value.ToLongTimeString() : String.Empty)}");

bool restartTimerElapsed = false;
bool modCheckTimerElapsed = false;

System.Timers.Timer modUpdateTimer = new System.Timers.Timer()
{
    Interval = TimeSpan.FromMinutes(30).TotalMilliseconds,
    AutoReset = true
};

modUpdateTimer.Elapsed += ModUpdateTimerElapsed;
modUpdateTimer.Start();

try
{
    // -------------- Main Loop ----------------------------------------------------------------------
    while (true)
    {
        if (modCheckTimerElapsed)
        {
            modCheckTimerElapsed = false;
            // int modsToUpdate = await modManager.DownloadModUpdatesViaSteamAsync();

            // Is an update available / are the local directories out of sync?
            if(!modManager.ModUpdateAvailable)
                continue;

            await logger.WriteLineAsync($"Mods need an update. Restarting in 5 Minutes!");

            await Task.Delay(TimeSpan.FromMinutes(5));

            await logger.WriteLineAsync("Stopping server now...");
            dayZServerHelper.StopServer();
            dayZServerHelper.StopRestartTimer();

            await Task.Delay(TimeSpan.FromSeconds(20));

            await logger.WriteLineAsync("Syncing Workshop-Mod-Folder with Server-Mod-Folder...", false);

            int syncedModsLocal = await modManager.SyncWorkshopWithServerModsAsync();
            await logger.WriteLineAsync($"Synced {syncedModsLocal} Mod(s) locally");

            await logger.WriteLineAsync($"Restarting server now.");
            dayZServerHelper.StartServer(modManager.ServerFolderModDirectoryNames);
            dayZServerHelper.StartRestartTimer();

            await logger.WriteLineAsync($"Server started! Next restart scheduled at " +
                                        $"{(dayZServerHelper.TimeOfNextRestart.HasValue ? dayZServerHelper.TimeOfNextRestart.Value.ToLongTimeString() : String.Empty)}");

            continue;
        }

        if (restartTimerElapsed)
        {
            restartTimerElapsed = false;

            await logger.WriteLineAsync("Server Restart-Timer Elapsed, restarting now.");

            Console.WriteLine($"Stopping server now.");
            dayZServerHelper.StopServer();
            dayZServerHelper.StopRestartTimer();
            await Task.Delay(TimeSpan.FromSeconds(20));

            await logger.WriteLineAsync($"Checking for DayZServer Updates...", false);
            // await steamApiWrapper.UpdateDayZServer();

            Console.WriteLine($"Restarting server now.");
            dayZServerHelper.StartServer(modManager.ServerFolderModDirectoryNames);
            dayZServerHelper.StartRestartTimer();

            await logger.WriteLineAsync($"Server started! Next restart scheduled: " +
                                        $"{(dayZServerHelper.TimeOfNextRestart.HasValue ? dayZServerHelper.TimeOfNextRestart.Value.ToLongTimeString() : String.Empty)}");

            continue;
        }

        await logger.WriteLineAsync($"Next restart in {Math.Round(dayZServerHelper.TimeUntilNextRestart.Value.TotalMinutes, 2)} mins", false);
        await Task.Delay(TimeSpan.FromSeconds(30));

        if(!dayZServerHelper.IsRunning && !restartTimerElapsed && !modCheckTimerElapsed)
        {
            await logger.WriteLineAsync($"Server crashed, restarting.");

            dayZServerHelper.StopServer();
            dayZServerHelper.StopRestartTimer();
            await Task.Delay(TimeSpan.FromSeconds(20));

            await logger.WriteLineAsync($"Restarting server now.");

            dayZServerHelper.StartServer(modManager.ServerFolderModDirectoryNames);
            dayZServerHelper.StartRestartTimer();

            await logger.WriteLineAsync("Server started! Next restart scheduled: " +
                                        $"{(dayZServerHelper.TimeOfNextRestart.HasValue ? dayZServerHelper.TimeOfNextRestart.Value.ToLongTimeString() : String.Empty)}");
        }
    }
}
catch(Exception ex)
{
    Console.WriteLine(ex.Message);
    Console.ReadLine();
    return 1;
}

void DayZServerHelperRestartTimerElapsed()
{
    restartTimerElapsed = true;
    Console.WriteLine($"RestartTimer Elapsed!");
}

void ModUpdateTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
{
    modCheckTimerElapsed = true;
    Console.WriteLine($"Checking for mod updates..");
}