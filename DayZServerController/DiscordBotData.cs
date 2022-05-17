using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayZServerController
{
    public class DiscordBotData
    {
        public string? Token { get; } = String.Empty;
        public ulong ChannelId { get; }
        public bool IsDataValid { get; } = true;

        public DiscordBotData(FileInfo discordSecretFile)
        {
            if (!discordSecretFile.Exists)
            {
                IsDataValid = false;
                return;
            }

            try
            {
                using (StreamReader sr =
                       new StreamReader(discordSecretFile.FullName))
                {
                    Token = sr.ReadLine();

                    if (!ulong.TryParse(sr.ReadLine(), out var channelId))
                    {
                        throw new IOException($"Discord channel ID not valid.");
                    }

                    ChannelId = channelId;

                    if (String.IsNullOrEmpty(Token))
                        throw new IOException(
                            $"Discord token/channel ID could not be read from file. ({discordSecretFile.FullName})");
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"DiscordBot Error: {ex.Message}");
                IsDataValid = false;
            }
        }
    }
}
