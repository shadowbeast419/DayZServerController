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
        private string _token = "OTQ4NjExNTM0NjIyNDU3ODg2.Yh-VVg.5FJCX9gvQEn-wCPRDMh_QVCxwpY";
        private ulong _channelID = 948611135953838121;

        public DiscordBot()
        {
            _client = new DiscordSocketClient();
        }

        public async Task Init()
        {
            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();
        }

        public async Task Announce(string message)
        {
            var channel = await _client.GetChannelAsync(_channelID) as IMessageChannel;
            await channel.SendMessageAsync(message);
        }

        ~DiscordBot()
        {
            _client.Dispose();
        }
    }
}
