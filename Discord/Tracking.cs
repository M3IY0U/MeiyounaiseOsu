using System;
using System.Collections.Generic;
using System.IO;
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
    [RequireUserPermissions(Permissions.ManageMessages)]
    [Group("track")]
    public class Tracking : BaseCommandModule
    {
        private static OsuClient _client = new OsuClient(new OsuSharpConfiguration
        {
            ApiKey = Utilities.GetKey("osu"),
            ModeSeparator = " | "
        });

        private InteractivityExtension Interactivity;

        public Tracking(OsuClient client)
        {
            _client = client;
            Interactivity = Bot.Client.GetInteractivity();
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
            if (response.Result.Content == "confirm")
            {
                DataStorage.GetGuild(ctx.Guild).TrackedUsers.Clear();
                DataStorage.SaveGuilds();
            }
            else
                await ctx.RespondAsync("Didn't receive confirmation or timed out, aborting.");
        }

        public static async Task FetchTopPlays(GuildDownloadCompletedEventArgs guildDownloadCompletedEventArgs)
        {
            try
            {
                var file = new StreamReader("update.txt");
                var chn = await Bot.Client.GetChannelAsync(Convert.ToUInt64(file.ReadLine()));
                await chn.SendMessageAsync(
                    $"Back online.\nRestart took {Math.Round(DateTime.Now.Subtract(DateTime.Parse(file.ReadLine()!)).TotalSeconds),2} seconds.");
                file.Close();
                File.Delete("update.txt");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Tried to announce time it took to restart but couldn't because of: {ex.Message}");
            }

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
                    if (userInfo.PerformancePoints != null)
                        DataStorage.GetUser(trackedUser).Pp = userInfo.PerformancePoints.Value;
                    if (userInfo.Rank != null) DataStorage.GetUser(trackedUser).Rank = userInfo.Rank.Value;
                }
            }
        }

        public static async void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            var usersToUpdate = new Dictionary<string, KeyValuePair<long, double>>();
            foreach (var guild in DataStorage.Guilds.Where(guild =>
                guild.TrackedUsers.Count != 0 && guild.OsuChannel != 0))
            {
                foreach (var (user, oldTop) in guild.TopPlays)
                {
                    IReadOnlyList<Score> newTop;
                    try
                    {
                        newTop = await _client.GetUserBestsByUsernameAsync(user, GameMode.Standard, 50);

                        if (newTop.SequenceEqual(oldTop))
                            continue;

                        for (var i = 0; i < newTop.Count; i++)
                        {
                            var play = newTop[i];
                            if (play.BeatmapId == oldTop[i].BeatmapId)
                                continue;

                            var map = await play.GetBeatmapAsync();
                            var player = await play.GetUserAsync();
                            var ssData = await OppaiClient.GetPPAsync(map.BeatmapId, play.Mods, 100, map.MaxCombo);

                            var isDt = play.Mods.ToString().ToLower().Contains("doubletime") ||
                                       play.Mods.ToString().ToLower().Contains("nightcore");
                            var ssText = play.Rank != "SS" ? $" *({Math.Round(ssData.Pp, 2)}pp for SS)*" : "";
                            var gain = Math.Round(player.PerformancePoints.Value - DataStorage.GetUser(user).Pp, 2);

                            if (gain > 0)
                            {
                                var eb = new DiscordEmbedBuilder()
                                    .WithAuthor($"New #{i + 1} for {user}!", $"https://osu.ppy.sh/users/{play.UserId}",
                                        $"http://s.ppy.sh/a/{play.UserId}")
                                    .WithThumbnail(map.ThumbnailUri)
                                    .WithColor(DiscordColor.Gold)
                                    .WithDescription(
                                        $"» **[{map.Title} [{map.Difficulty}]](https://osu.ppy.sh/b/{map.BeatmapId})**\n" +
                                        $"» **{Math.Round(ssData.Stars, 2)}★** » {TimeSpan.FromSeconds(!isDt ? map.TotalLength.TotalSeconds : map.TotalLength.TotalSeconds / 1.5):mm\\:ss} » {(!isDt ? map.Bpm : map.Bpm * 1.5)}bpm » +{play.Mods}\n" +
                                        $"» {DiscordEmoji.FromName(Bot.Client, $":{play.Rank}_Rank:")} » **{Math.Round(play.Accuracy, 2)}%** » **{Math.Round(play.PerformancePoints ?? 0.0, 2)}pp** » {ssText}\n" +
                                        $"» {play.TotalScore} » x{play.MaxCombo}/{map.MaxCombo} » [{play.Count300}/{play.Count100}/{play.Count50}/{play.Miss}]\n" +
                                        $"» {Math.Round(DataStorage.GetUser(user).Pp, 2)}pp ⇒ **{Math.Round(player.PerformancePoints.Value, 2)}pp** ({(gain > 0 ? "+" : "")}{gain}pp)\n" +
                                        $"» #{DataStorage.GetUser(user).Rank} ⇒ **#{player.Rank}** ({player.Country.TwoLetterISORegionName}#{player.CountryRank})\n")
                                    .WithFooter("Submitted " + play.Date?.Humanize());

                                var channel = await Bot.Client.GetChannelAsync(guild.OsuChannel);
                                await channel.SendMessageAsync(embed: eb.Build());
                                guild.UpdateBeatmapInChannel(channel.Id, map.BeatmapId);
                            }

                            guild.TopPlays.TryUpdate(user, newTop.ToList(), oldTop);
                            if (!usersToUpdate.ContainsKey(user))
                                usersToUpdate.Add(user,
                                    new KeyValuePair<long, double>(player.Rank.Value, player.PerformancePoints.Value));
                            break;
                        }
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(
                            $"Something went wrong trying to get {user}'s top plays: {exception.Message}");
                        var chn = await Bot.Client.GetChannelAsync(493557274371948545);
                        await chn.SendMessageAsync(
                            $"Something went wrong trying to get {user}'s top plays: {exception.Message}");
                    }
                }
            }

            foreach (var (user, (rank, pp)) in usersToUpdate)
            {
                DataStorage.GetUser(user).UpdateRank(rank, pp);
            }
        }
    }
}