
using DayZServerController;
using System.Diagnostics;

// Argument1: SteamApiPath
// Argument2: ModlistPath
// Argument3: Path to DayZServer-Executable
// Argument4: Restart Period in s
// Argument5: DayZ-Mods Folder

// -------------- Checking CLI Arguments ---------------------------------------

bool skipModCheckAtStartup = false;
bool muteDiscordBot = false;

if (args.Length != Enum.GetNames(typeof(CLIArgumentsIndices)).Length)
{
    Console.WriteLine($"Invalid number of CLI-Arguments. Is: {args.Length}, " +
        $"Should: {Enum.GetNames(typeof(CLIArgumentsIndices)).Length}. Exiting application...");
    Console.ReadLine();

    return 1;
}

SteamApiWrapper steamApiWrapper;
ModCopyHelper modCopyHelper;
DayZServerHelper dayZServerHelper;
DiscordBot discordBot = new DiscordBot();

try
{
    steamApiWrapper = new SteamApiWrapper(args[(int)CLIArgumentsIndices.SteamApiPath]);

    modCopyHelper = new ModCopyHelper(args[(int)CLIArgumentsIndices.ModsFolder], args[(int)CLIArgumentsIndices.DayZServerExecPath],
        new ModlistReader(args[(int)CLIArgumentsIndices.ModlistPath]),
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

bool serverWasAlreadyRunning = dayZServerHelper.IsRunning;

if(!muteDiscordBot)
{
    await discordBot.Init();
    // await discordBot.Announce($"Hello, ServerBot is now ready for tasks! (DayZController (tm) started)");
}

if (serverWasAlreadyRunning)
{
    Console.Write("Server is already running: Do you want to restart the process? (y/n)");
    
    ConsoleKeyInfo pressedKey = Console.ReadKey();
    bool stopServer = pressedKey.Key == ConsoleKey.Y;

    if(stopServer)
    {
        // Server is already running at startup of this exec -> stop it and restart with fitting restart interval
        dayZServerHelper.StopServer();
        await Task.Delay(5000);

        await modCopyHelper.InitWatchingAsync();
        await CheckForModsAndUpdate();

        // Start restart timer and server
        dayZServerHelper.StartServer(modCopyHelper.ModDestinationFolders);

        dayZServerHelper.RestartTimerElapsed += DayZServerHelper_RestartTimerElapsed;
        dayZServerHelper.StartRestartTimer();
    }
    else
    {
        Console.WriteLine("Application will now exit...");
        Console.ReadLine();
        return 1;
    }
}
else
{
    await modCopyHelper.InitWatchingAsync();

    if (!skipModCheckAtStartup)
    {
        await CheckForModsAndUpdate();
    }

    // Start restart timer and server
    dayZServerHelper.StartServer(modCopyHelper.ModDestinationFolders);

    dayZServerHelper.RestartTimerElapsed += DayZServerHelper_RestartTimerElapsed;
    dayZServerHelper.StartRestartTimer();

    if(!muteDiscordBot)
    {
        await discordBot.Announce($"Server started! Next restart scheduled at " +
    $"{(dayZServerHelper.TimeOfNextRestart.HasValue ? dayZServerHelper.TimeOfNextRestart.Value.ToLongTimeString() : String.Empty)}");
    }
}

bool serverNeedsRestart = false;
bool checkForMods = false;

System.Timers.Timer modUpdateTimer = new System.Timers.Timer()
{
    Interval = 1800 * 1000,
    AutoReset = true
};

modUpdateTimer.Elapsed += ModUpdateTimer_Elapsed;
modUpdateTimer.Start();

try
{
    // -------------- Main Loop ----------------------------------------------------------------------
    while (true)
    {
        if (checkForMods)
        {
            checkForMods = false;
            int modsToUpdate = await modCopyHelper.CheckForUpdatesAsync();

            if (modsToUpdate == 0)
            {
                Console.WriteLine($"All mods are up-to date!");
            }
            else
            {
                if(!muteDiscordBot)
                    await discordBot.Announce($"{modsToUpdate} Mods need an update. Restarting in 5 Minutes!");

                Console.WriteLine($"{modsToUpdate} mods need to be updated! Restarting server in 5 minutes!");
                await Task.Delay(1000 * 60 * 5);

                Console.WriteLine($"Stopping server now.");

                if (!muteDiscordBot)
                    await discordBot.Announce($"Stopping server now...");

                dayZServerHelper.StopServer();
                dayZServerHelper.StopRestartTimer();
                await Task.Delay(20000);

                Console.WriteLine($"Copying updated mods to server directory...");
                int updatedMods = await modCopyHelper.UpdateModsAsync();
                Console.WriteLine($"Updated {updatedMods} mods!");

                Console.WriteLine($"Restarting server now.");
                dayZServerHelper.StartServer(modCopyHelper.ModDestinationFolders);
                dayZServerHelper.StartRestartTimer();

                if (!muteDiscordBot)
                    await discordBot.Announce($"Server started!");
            }
        }

        if (serverNeedsRestart)
        {
            serverNeedsRestart = false;

            //if (!muteDiscordBot)
            //    await discordBot.Announce($"Server Restart-Timer Elapsed, restarting now.");

            Console.WriteLine($"Stopping server now.");
            dayZServerHelper.StopServer();
            dayZServerHelper.StopRestartTimer();
            await Task.Delay(20000);

            Console.WriteLine($"Restarting server now.");
            dayZServerHelper.StartServer(modCopyHelper.ModDestinationFolders);
            dayZServerHelper.StartRestartTimer();

            if (!muteDiscordBot)
                await discordBot.Announce($"Server started! Next restart scheduled: " +
                $"{(dayZServerHelper.TimeOfNextRestart.HasValue ? dayZServerHelper.TimeOfNextRestart.Value.ToLongTimeString() : String.Empty)}");
        }

        Console.WriteLine($"Next restart in {Math.Round(dayZServerHelper.TimeUntilNextRestart.Value.TotalMinutes, 2)} mins");
        await Task.Delay(10000);

        if(!dayZServerHelper.IsRunning)
        {
            Console.WriteLine($"Server crashed, restarting.");
            await discordBot.Announce($"Server crashed, restarting.");
            dayZServerHelper.StopServer();
            dayZServerHelper.StopRestartTimer();

            await Task.Delay(20000);
            Console.WriteLine($"Restarting server now.");
            dayZServerHelper.StartServer(modCopyHelper.ModDestinationFolders);
            dayZServerHelper.StartRestartTimer();
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
    serverNeedsRestart = true;
    Console.WriteLine($"RestartTimer Elapsed!");
}

void ModUpdateTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
{
    checkForMods = true;
    Console.WriteLine($"Checking for mod updates..");
}

async Task CheckForModsAndUpdate()
{
    int modsToUpdate = await modCopyHelper.CheckForUpdatesAsync();

    if (modsToUpdate > 0)
    {
        int updatedMods = await modCopyHelper.UpdateModsAsync();
    }
}