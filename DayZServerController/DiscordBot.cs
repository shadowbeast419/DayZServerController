using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace DayZServerController
{
    internal class DiscordBot
    {
        private DiscordSocketClient _client;
        private DiscordBotData _botData;
        private bool _isInit;

        public bool Mute { get; set; } = false;

        public DiscordBot(DiscordBotData botData)
        {
            if (!botData.IsDataValid)
                return;

            _client = new DiscordSocketClient();
            _isInit = true;
        }

        public async Task Init()
        {
            if (!_isInit)
                return;

            await _client.LoginAsync(TokenType.Bot, _botData.Token);
            await _client.StartAsync();
        }

        public async Task Announce(string message)
        {
            if (!_isInit || Mute)
                return;

            var channel = await _client.GetChannelAsync(_botData.ChannelId) as IMessageChannel;
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
