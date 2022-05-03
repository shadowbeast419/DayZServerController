# DayZServerController .NET
 A handy .NET tool for every DayZ-Server owner

Disclaimer:
I am already using this tool for my server (AustrianDayZ #1 Chernarus - Persistance ON - Expansion Mod - AI), there are some issues with ModUpdates but I don't know yet what the problem is. I suspect the SteamCMD. However, it runs stable!

This tool is still in BETA, some configuration still have to take place inside the code and there are some bugs left. 
Just send me a request if you need any assistance with my tool. Feedback is always welcome!

Features:
- Checks periodically for updates of Mods and DayZ Server itself
- Handles Synchroniziation of Steam Workshop Folder and DayZServer Mod Folder (using a user-defined Modlist.txt)
- Discord Bot for Restart-Messages/Mod-Update Notifications
- Automatically restarts Server every n Hours
- Checks regularly for Mod Updates via SteamCMD and restarts Server if there are any

Usage Manual:

- Create a Modlist.txt which maps the Steam Mod-IDs with the corresponding folders inside your DayZServer-Directory

Example of Modlist.txt

```
1644467354,@SummerChernarus
2224593910,@WornRepair
1582756848,@ZomBerryAdminTools
1912237302,@IRP-Land-Rover-Defender-110
```

- Create .bat file or shortcut to start DayZServerController.exe with the necessary CLI arguments.

Example of DayZServerController.bat:

```
start "" DayZServerController.exe "C:\Users\tacticalbacon\Documents\steamcmd\steamcmd.exe" "C:\Program Files (x86)\Steam\steamapps\common\DayZServer\Modlist.txt" "C:\Program Files (x86)\Steam\steamapps\common\DayZServer\DayZServer_x64.exe" 14400 "C:\Program Files (x86)\Steam\steamapps\workshop\content\221100"
```

CLI Argument sequence is defined in CliArgumentsIndices.cs

```
SteamApiPath = 0,
ModlistPath = 1,
DayZServerExecPath = 2,
RestartPeriod = 3 (in seconds),
WorkshopFolder = 4
```

- The discord bot needs a file with your token and channelID to post something (you don't know what that is? Please look it up for yourself, sry). This path is still hardcoded (for a lack of more time from my side), you need to change it for your setup. The contets look like follows:

First line is the token, second line is the channelID

```
OTQ4NjExNTM0NjIyNXXXXXXXXXXXXXg4gxGGn4kBDv1FT5ePQfpU
94861XXXXXXXX38121
```

Inside DiscordBot.cs change the path to your token file:

```
private readonly string _discordDataFilePath = 
    @"C:\Users\tacticalbacon\Documents\Github\DayZServerController\token.txt";
```

- Inside the code in Program.cs you can change two booleans to your needs

```
bool checkModsAtStartup = false;
bool muteDiscordBot = false;

```

- At the first start, you will be prompted to enter your Steam Credentials for SteamCMD. This is necessary to get Mod/Server Updates. You should have to enter these credentials only once, they will be stored safely in the Windows-Credentials-Manager for future startups of this tool.

Future Plans:

- No more changes in code necessary, everything via CLI Arguments
- Automatical Server Backups before each ModUpdate/Server Update
...

Last but not least, some advertising for my server for the server owners like me who also like to play the game -> discord.io/AustrianDayZ
