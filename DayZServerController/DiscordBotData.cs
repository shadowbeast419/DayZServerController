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
        public ulong ChannelId { get; } = 0;
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
                    if (!ulong.TryParse(sr.ReadLine(), out var channelId) || ChannelId == 0)
                    {
                        throw new IOException($"Discord channel ID not valid. ({channelId})");
                    }

                    ChannelId = channelId;

                    Token = sr.ReadLine();

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
