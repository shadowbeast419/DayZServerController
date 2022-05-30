using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayZServerController
{
    internal class Logging
    {
        private DiscordBot _discordBot;

        public bool MuteDiscordBot { get; set; } = false;
        public bool MuteConsole { get; set; } = false;

        public Logging(DiscordBot discordBot)
        {
            _discordBot = discordBot;
        }

        public async Task WriteLineAsync(string msg, bool writeToDiscord = true)
        {
            if (!MuteConsole)
                Console.WriteLine(msg);

            if (!MuteDiscordBot && writeToDiscord)
                await _discordBot.Announce(msg);
        }
    }
}
