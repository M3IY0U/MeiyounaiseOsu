using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using Humanizer;
using MeiyounaiseOsu.Core;
using OsuSharp;
using OsuSharp.Oppai;
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

        [RequireUserPermissions(Permissions.Administrator)]
        [Group("track")]
        internal class Tracking
        {
            private static OsuClient _client;
            private InteractivityModule Interactivity;

            public Tracking(OsuClient client, InteractivityModule interactivity)
            {
                _client = client;
                Interactivity = interactivity;
            }

            [Command("add")]
            public async Task TrackAdd(CommandContext ctx, string username)
            {
                var check = await _client.GetUserByUsernameAsync(username, GameMode.Standard);
                if (check == null)
                    throw new Exception($"No user with name {username} was found!");
                DataStorage.GetGuild(ctx.Guild).TrackedUsers.Add(username);
                DataStorage.SaveGuilds();
                await Utilities.ConfirmCommand(ctx.Message);
            }

            [Command("remove")]
            public async Task TrackRemove(CommandContext ctx, string user)
            {
                DataStorage.GetGuild(ctx.Guild).TrackedUsers.Remove(user);
                DataStorage.SaveGuilds();
                await Utilities.ConfirmCommand(ctx.Message);
            }

            [Command("list")]
            public async Task TrackList(CommandContext ctx)
            {
                var users = DataStorage.GetGuild(ctx.Guild).TrackedUsers;
                if (users.Count == 0)
                {
                    await ctx.RespondAsync("Currently not tracking anyone in this guild!");
                    return;
                }

                var list = users.Select(user => $"[{user}](https://osu.ppy.sh/users/{user})").ToList();

                var eb = new DiscordEmbedBuilder()
                    .WithTitle("Tracked users")
                    .WithColor(new DiscordColor(220, 152, 164))
                    .WithDescription(string.Join(", ", list));
                await ctx.RespondAsync(embed: eb.Build());
            }

            [Command("clear")]
            public async Task TrackClear(CommandContext ctx)
            {
                await ctx.RespondAsync("Are you sure you want to clear the list? Please confirm by typing `confirm`.");
                var response = await Interactivity.WaitForMessageAsync(message => message.Author == ctx.Message.Author);
                if (response.Message.Content == "confirm")
                {
                    DataStorage.GetGuild(ctx.Guild).TrackedUsers.Clear();
                    DataStorage.SaveGuilds();
                }
                else
                    await ctx.RespondAsync("Didn't receive confirmation or timed out, aborting.");
            }

            public static async Task FetchTopPlays(ReadyEventArgs e)
            {
                foreach (var guild in DataStorage.Guilds)
                {
                    if (guild.TrackedUsers.Count == 0 || guild.OsuChannel == 0)
                        continue;
                    foreach (var trackedUser in guild.TrackedUsers)
                    {
                        var userBest = await _client.GetUserBestsByUsernameAsync(trackedUser, GameMode.Standard, 50);
                        guild.TopPlays.TryAdd(trackedUser, userBest.ToList());
                        if (DataStorage.GetUser(trackedUser) == null)
                            DataStorage.CreateUser(trackedUser);
                        var userInfo = await _client.GetUserByUsernameAsync(trackedUser, GameMode.Standard);
                        DataStorage.GetUser(trackedUser).Pp = userInfo.PerformancePoints;
                        DataStorage.GetUser(trackedUser).Rank = userInfo.Rank;
                    }
                }
            }

            public static async void TimerElapsed(object sender, ElapsedEventArgs e)
            {
                Console.WriteLine("Polling for new top plays");
                foreach (var guild in DataStorage.Guilds)
                {
                    foreach (var (user, oldTop) in guild.TopPlays)
                    {
                        var newTop = await _client.GetUserBestsByUsernameAsync(user, GameMode.Standard, 50);
                        if (newTop.SequenceEqual(oldTop))
                            continue;

                        for (var i = newTop.Count - 1; i >= 0; i--)
                        {
                            var play = newTop[i];
                            if (play.BeatmapId == oldTop[i].BeatmapId)
                                continue;

                            var map = await play.GetBeatmapAsync();
                            var player = await play.GetUserAsync();
                            var ssData = await OppaiClient.GetPPAsync(map.BeatmapId, play.Mods, 100f, map.MaxCombo);

                            var isDt = play.Mods.ToString().ToLower().Contains("doubletime") ||
                                       play.Mods.ToString().ToLower().Contains("nightcore");
                            var ssText = play.Rank != "SS" ? $" ({Math.Round(ssData.Pp, 2)}pp for SS)" : "";
                            var gain = player.PerformancePoints - DataStorage.GetUser(user).Pp;
                            var gainText = gain > 0 ? $"+{gain}" : $"-{gain}";

                            var eb = new DiscordEmbedBuilder()
                                .WithAuthor($"New #{i + 1} for {user}!", $"https://osu.ppy.sh/users/{play.UserId}",
                                    $"http://s.ppy.sh/a/{play.UserId}")
                                .WithThumbnailUrl(map.ThumbnailUri)
                                .WithColor(DiscordColor.Gold)
                                .WithDescription(
                                    $"» **[{map.Title} [{map.Difficulty}]](https://osu.ppy.sh/b/{map.BeatmapId})**\n" +
                                    $"» **{Math.Round(ssData.Stars, 2)}★** » {TimeSpan.FromSeconds(!isDt ? map.TotalLength.TotalSeconds : map.TotalLength.TotalSeconds / 1.5):mm\\:ss} » {(!isDt ? map.Bpm : map.Bpm * 1.5)}bpm » +{play.Mods}\n" +
                                    $"» {DiscordEmoji.FromName(Bot.Client, $":{play.Rank}_Rank:")} » **{Math.Round(play.Accuracy, 2)}%** » **{Math.Round(play.PerformancePoints, 2)}pp** ({gainText})\n" +
                                    $"» {play.TotalScore} » x{play.MaxCombo}/{map.MaxCombo} » [{play.Count300}/{play.Count100}/{play.Count50}/{play.Miss}]\n" +
                                    $"» {ssText} » #{DataStorage.GetUser(user).Rank} ⇒ #{player.Rank} ({player.Country.TwoLetterISORegionName}#{player.CountryRank})\n")
                                .WithFooter(play.Date.Humanize());
                            var channel = await Bot.Client.GetChannelAsync(guild.OsuChannel);
                            await channel.SendMessageAsync(embed: eb.Build());
                            guild.TopPlays.TryUpdate(user, newTop.ToList(), oldTop);
                            DataStorage.GetUser(user).UpdateRank(player.Rank, player.PerformancePoints);
                        }
                    }
                }
            }
        }
    }
}