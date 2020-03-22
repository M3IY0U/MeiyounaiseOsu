using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using MeiyounaiseOsu.Core;
using OsuSharp;

namespace MeiyounaiseOsu.Discord
{
    public class General
    {
        public OsuClient Client;

        public General(OsuClient client)
        {
            Client = client;
        }

        [Command("osuset")]
        public async Task SetUser(CommandContext ctx, string username)
        {
            var user = await Client.GetUserByUsernameAsync(username, GameMode.Standard);
            if (user == null)
                throw new Exception($"No user with name {username} was found!");
            DataStorage.CreateUser(ctx.User, user.Username);
            await Utilities.ConfirmCommand(ctx.Message);
        }
    }
}