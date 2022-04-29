using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayZServerController
{
    internal class DiscordBot
    {
        private DiscordSocketClient _client;
        private string _token = String.Empty;
        private bool _isInit;
        private ulong _channelID;
        private readonly string _discordDataFilePath = 
            @"C:\Users\tacticalbacon\Documents\Github\DayZServerController\token.txt";

        public DiscordBot()
        {
            _client = new DiscordSocketClient();
            _isInit = false;
            _channelID = 0;

            try
            {
                using (StreamReader sr =
                       new StreamReader(_discordDataFilePath))
                {
                    _token = sr.ReadLine();
                    _channelID = ulong.Parse(sr.ReadLine());
                }

                if (String.IsNullOrEmpty(_token) || _channelID == 0)
                    throw new IOException(
                        $"Discord token/channel ID could not be read from file. ({_discordDataFilePath})");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"DiscordBot Error: {ex.Message}");
            }

            _isInit = true;
        }

        public async Task Init()
        {
            if (!_isInit)
                return;

            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();
        }

        public async Task Announce(string message)
        {
            if (!_isInit)
                return;

            var channel = await _client.GetChannelAsync(_channelID) as IMessageChannel;
            await channel.SendMessageAsync(message);
        }

        ~DiscordBot()
        {
            if (!_isInit)
                return;

            _client?.Dispose();
        }
    }
}
