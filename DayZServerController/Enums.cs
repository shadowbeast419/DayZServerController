using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayZServerController
{
    public enum CliArgumentIndices : int
    {
        SteamCmdPath = 0,
        DiscordFilePath,
        ModlistPath,
        DayZServerExecPath,
        DayZGameExecPath,
        RestartPeriod,
        WorkshopFolder
    };

    public enum SteamCmdModeEnum
    {
        SteamCmdExe,
        SteamPowerShellWrapper,
        Disabled
    }
}
