using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Humanizer;
using MeiyounaiseOsu.Core;
using OsuSharp;

namespace MeiyounaiseOsu.Discord
{
    public class Profile : BaseCommandModule
    {
        private OsuClient Client;

        public Profile(OsuClient client)
        {
            Client = client;
        }

        [Command("osu")]
        public async Task OsuProfile(CommandContext ctx, string username = "")
        {
            await ShowProfile(ctx, username, GameMode.Standard);
        }

        [Command("taiko")]
        public async Task TaikoProfile(CommandContext ctx, string username = "")
        {
            await ShowProfile(ctx, username, GameMode.Taiko);
        }

        [Command("mania")]
        public async Task ManiaProfile(CommandContext ctx, string username = "")
        {
            await ShowProfile(ctx, username, GameMode.Mania);
        }

        [Command("catch")]
        public async Task CatchProfile(CommandContext ctx, string username = "")
        {
            await ShowProfile(ctx, username, GameMode.Catch);
        }

        private async Task ShowProfile(CommandContext ctx, string username, GameMode gameMode)
        {
            if (string.IsNullOrEmpty(username))
            {
                try
                {
                    username = DataStorage.GetUser(ctx.User).OsuUsername;
                }
                catch (Exception)
                {
                    username = null;
                }
            }
            
            if (string.IsNullOrEmpty(username))
                throw new Exception("I have no username set for you! Set it using `osuset [name]`.");

            var profile = await Client.GetUserByUsernameAsync(username, gameMode);
            if (profile == null)
                throw new Exception($"No profile with the name {username} was found!");
            var eb = new DiscordEmbedBuilder()
                .WithAuthor($"{profile.Username}'s osu! {gameMode.Humanize()} profile",
                    $"https://osu.ppy.sh/users/{profile.UserId}")
                .WithColor(new DiscordColor(220, 152, 164))
                .WithThumbnail($"http://s.ppy.sh/a/{profile.UserId}")
                .WithDescription($"**Rank** » #{profile.Rank} ({profile.Country}: #{profile.CountryRank})\n" +
                                 $"**PP** » {Math.Round(profile.PerformancePoints ?? 0)}\n" +
                                 $"**Accuracy** » {Math.Round(profile.Accuracy ?? 0, 2)}%\n" +
                                 $"**Playcount** » {profile.PlayCount}\n" +
                                 $"**Joined** » {profile.JoinDate.Humanize()}\n");

            await ctx.RespondAsync(embed: eb.Build());
        }
    }
}