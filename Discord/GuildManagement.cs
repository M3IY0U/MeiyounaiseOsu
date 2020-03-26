using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MeiyounaiseOsu.Core;
using Utilities = MeiyounaiseOsu.Core.Utilities;

namespace MeiyounaiseOsu.Discord
{
    public class GuildManagement
    {
        [Command("prefix")]
        public async Task Prefix(CommandContext ctx, string prefix = "")
        {
            if (string.IsNullOrEmpty(prefix))
            {
                await ctx.RespondAsync($"The prefix on this guild is: `{DataStorage.GetGuild(ctx.Guild).Prefix}`");
                return;
            }

            DataStorage.GetGuild(ctx.Guild).Prefix = prefix;
            DataStorage.SaveGuilds();
            await Utilities.ConfirmCommand(ctx.Message);
        }

        [Command("trackingchannel"), Aliases("tc")]
        public async Task TrackingChannel(CommandContext ctx, DiscordChannel channel = null)
        {
            var guildChannel = DataStorage.GetGuild(ctx.Guild).OsuChannel;
            if (channel == null)
            {
                await ctx.RespondAsync(guildChannel == 0
                    ? "Currently not tracking top plays in any channel!"
                    : $"Currently tracking top plays in channel <#{guildChannel}>");
                return;
            }

            DataStorage.GetGuild(ctx.Guild).OsuChannel = channel.Id;
            DataStorage.SaveGuilds();
            await Utilities.ConfirmCommand(ctx.Message);
        }
    }
}