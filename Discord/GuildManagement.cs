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
        public async Task TrackingChannel(CommandContext ctx, DiscordChannel channel = null, string disable = "")
        {
            var guildChannel = DataStorage.GetGuild(ctx.Guild).OsuChannel;

            if (disable == "disable")
            {
                DataStorage.GetGuild(ctx.Guild).OsuChannel = 0;
                DataStorage.SaveGuilds();
                await Utilities.ConfirmCommand(ctx.Message);
                return;
            }

            if (channel == null)
            {
                await ctx.RespondAsync(guildChannel == 0
                    ? $"Currently not tracking top plays in any channel! (If you want to disable tracking, use `{DataStorage.GetGuild(ctx.Guild).Prefix}tc <any channel> disable`)"
                    : $"Currently tracking top plays in channel <#{guildChannel}>. (If you want to disable tracking, use `{DataStorage.GetGuild(ctx.Guild).Prefix}tc <any channel> disable`)");
                return;
            }

            DataStorage.GetGuild(ctx.Guild).OsuChannel = channel.Id;
            DataStorage.SaveGuilds();
            await Utilities.ConfirmCommand(ctx.Message);
        }
    }
}