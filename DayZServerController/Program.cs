
using DayZServerController;
using System.Diagnostics;

// Argument1: SteamApiPath
// Argument2: ModlistPath
// Argument3: Path to DayZServer-Executable
// Argument4: Restart Period in s
// Argument5: DayZ-Mods Folder

// -------------- Checking CLI Arguments ---------------------------------------

bool checkModsAtStartup = true;
bool muteDiscordBot = false;

if (args.Length != Enum.GetNames(typeof(CLIArgumentsIndices)).Length)
{
    Console.WriteLine($"Invalid number of CLI-Arguments. Is: {args.Length}, " +
        $"Should: {Enum.GetNames(typeof(CLIArgumentsIndices)).Length}. Exiting application...");
    Console.ReadLine();

    return 1;
}

SteamApiWrapper steamApiWrapper;
ModManager modManager;
DayZServerHelper dayZServerHelper;
DiscordBot discordBot = new DiscordBot()
{
    Mute = muteDiscordBot
};

try
{
    steamApiWrapper = new SteamApiWrapper(new FileInfo(args[(int)CLIArgumentsIndices.SteamApiPath]));

    modManager = new ModManager(new DirectoryInfo(args[(int)CLIArgumentsIndices.WorkshopFolder]), 
        new FileInfo(args[(int)CLIArgumentsIndices.DayZServerExecPath]),
        new ModlistReader(new FileInfo(args[(int)CLIArgumentsIndices.ModlistPath])),
        steamApiWrapper);

    dayZServerHelper = new DayZServerHelper(args[(int)CLIArgumentsIndices.DayZServerExecPath], 
        args[(int)CLIArgumentsIndices.RestartPeriod]);
}
catch(ArgumentException ex)
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

// Update DayZ Server via SteamCMD (if update is available)
Console.WriteLine($"Checking for DayZServer Updates...");
await steamApiWrapper.UpdateDayZServer();

await modManager.SyncWorkshopWithServerModsAsync();

if (checkModsAtStartup)
{
    Console.WriteLine($"Checking for Mod Updates...");
    int modsUpdated = await modManager.DownloadModUpdatesViaSteamAsync();

    if (modsUpdated > 0)
    {
        Console.WriteLine($"{modsUpdated} updated Mod(s) found!");

        await modManager.SyncWorkshopWithServerModsAsync();
        await discordBot.Announce($"{modsUpdated} Mod(s) updated.");
    }
}

// Start restart timer and server
dayZServerHelper.StartServer(modManager.ServerFolderModDirectoryNames);

dayZServerHelper.RestartTimerElapsed += DayZServerHelper_RestartTimerElapsed;
dayZServerHelper.StartRestartTimer();

await discordBot.Announce($"Server started! Next restart scheduled at " +
    $"{(dayZServerHelper.TimeOfNextRestart.HasValue ? dayZServerHelper.TimeOfNextRestart.Value.ToLongTimeString() : String.Empty)}");

bool restartTimerElapsed = false;
bool modCheckTimerElapsed = false;

System.Timers.Timer modUpdateTimer = new System.Timers.Timer()
{
    Interval = TimeSpan.FromMinutes(30).TotalMilliseconds,
    AutoReset = true
};

modUpdateTimer.Elapsed += ModUpdateTimer_Elapsed;
modUpdateTimer.Start();

try
{
    // -------------- Main Loop ----------------------------------------------------------------------
    while (true)
    {
        if (modCheckTimerElapsed)
        {
            modCheckTimerElapsed = false;
            int modsToUpdate = await modManager.DownloadModUpdatesViaSteamAsync();

            if (modsToUpdate == 0)
            {
                Console.WriteLine($"All mods are up-to date!");
            }
            else
            {
                await discordBot.Announce($"{modsToUpdate} Mods need an update. Restarting in 5 Minutes!");
                Console.WriteLine($"{modsToUpdate} mods need to be updated! Restarting server in 5 minutes!");

                await Task.Delay(TimeSpan.FromMinutes(5));

                await discordBot.Announce($"Stopping server now...");
                Console.WriteLine($"Stopping server now.");

                dayZServerHelper.StopServer();
                dayZServerHelper.StopRestartTimer();
                await Task.Delay(TimeSpan.FromSeconds(20));

                Console.WriteLine($"Syncing Workshop-Mod-Folder with Server-Mod-Folder...");
                await modManager.SyncWorkshopWithServerModsAsync();

                // Server is stopped, use that time for checking for DayZServer-Updates
                Console.WriteLine($"Checking for DayZServer Updates...");
                await steamApiWrapper.UpdateDayZServer();

                Console.WriteLine($"Restarting server now.");
                dayZServerHelper.StartServer(modManager.ServerFolderModDirectoryNames);
                dayZServerHelper.StartRestartTimer();

                await discordBot.Announce($"Server started! Next restart scheduled at " + 
                    $"{(dayZServerHelper.TimeOfNextRestart.HasValue ? dayZServerHelper.TimeOfNextRestart.Value.ToLongTimeString() : String.Empty)}");
            }
        }

        if (restartTimerElapsed)
        {
            restartTimerElapsed = false;

            await discordBot.Announce($"Server Restart-Timer Elapsed, restarting now.");

            Console.WriteLine($"Stopping server now.");
            dayZServerHelper.StopServer();
            dayZServerHelper.StopRestartTimer();
            await Task.Delay(TimeSpan.FromSeconds(20));

            Console.WriteLine($"Checking for DayZServer Updates...");
            await steamApiWrapper.UpdateDayZServer();

            Console.WriteLine($"Restarting server now.");
            dayZServerHelper.StartServer(modManager.ServerFolderModDirectoryNames);
            dayZServerHelper.StartRestartTimer();

            await discordBot.Announce($"Server started! Next restart scheduled: " +
                $"{(dayZServerHelper.TimeOfNextRestart.HasValue ? dayZServerHelper.TimeOfNextRestart.Value.ToLongTimeString() : String.Empty)}");
        }

        Console.WriteLine($"Next restart in {Math.Round(dayZServerHelper.TimeUntilNextRestart.Value.TotalMinutes, 2)} mins");
        await Task.Delay(TimeSpan.FromSeconds(30));

        if(!dayZServerHelper.IsRunning && !restartTimerElapsed && !modCheckTimerElapsed)
        {
            Console.WriteLine($"Server crashed, restarting.");
            await discordBot.Announce($"Server crashed, restarting.");

            dayZServerHelper.StopServer();
            dayZServerHelper.StopRestartTimer();
            await Task.Delay(TimeSpan.FromSeconds(20));

            Console.WriteLine($"Restarting server now.");
            dayZServerHelper.StartServer(modManager.ServerFolderModDirectoryNames);
            dayZServerHelper.StartRestartTimer();

            await discordBot.Announce($"Server started! Next restart scheduled: " +
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

void DayZServerHelper_RestartTimerElapsed()
{
    restartTimerElapsed = true;
    Console.WriteLine($"RestartTimer Elapsed!");
}

void ModUpdateTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
{
    modCheckTimerElapsed = true;
    Console.WriteLine($"Checking for mod updates..");
}