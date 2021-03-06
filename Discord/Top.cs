using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using MeiyounaiseOsu.Core;
using OsuSharp;
using OsuSharp.Oppai;

namespace MeiyounaiseOsu.Discord
{
    public class Top : BaseCommandModule
    {
        private OsuClient Client;
        private InteractivityExtension Interactivity;

        public Top(OsuClient client) => (Client, Interactivity) = (client, Bot.Client.GetInteractivity());

        [Command("osutop")]
        public async Task OsuProfile(CommandContext ctx, [RemainingText] string args = "")
        {
            await ShowTop(ctx, args, GameMode.Standard);
        }

        [Command("taikotop"), Hidden]
        public async Task TaikoProfile(CommandContext ctx, [RemainingText] string args = "")
        {
            await ShowTop(ctx, args, GameMode.Taiko);
        }

        [Command("maniatop"), Hidden]
        public async Task ManiaProfile(CommandContext ctx, [RemainingText] string args = "")
        {
            await ShowTop(ctx, args, GameMode.Mania);
        }

        [Command("catchtop"), Hidden]
        public async Task CatchProfile(CommandContext ctx, [RemainingText] string args = "")
        {
            await ShowTop(ctx, args, GameMode.Catch);
        }

        private async Task ShowTop(CommandContext ctx, string args, GameMode mode)
        {
            var argList = args.Split(' ').ToList();
            var num = 0;

            if (args.Contains("-p"))
            {
                if (args.Contains("-a"))
                    throw new Exception("You can't use -a and -p in the same command!");

                var marker = argList.IndexOf("-p");
                if (marker == argList.Count)
                    throw new Exception();
                num = Convert.ToInt32(argList[marker + 1]);
                argList.RemoveAt(marker);
                argList.RemoveAt(marker);
            }

            var limit = 100;

            if (args.Contains("-a") || args.Contains("-r") || args.Contains("-rr"))
            {
                limit = 50;
                var marker = argList.FindIndex(a => a == "-r" || a == "-a" || a == "-rr");
                argList.RemoveAt(marker);
            }

            var username = argList.FirstOrDefault();
            
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
                throw new Exception(
                    $"I have no username set for you! Set it using `{DataStorage.GetGuild(ctx.Guild).Prefix}osuset [name]`.");

            var scores = await Client.GetUserBestsByUsernameAsync(username, mode, limit);

            //Show all plays (paginated)
            if (args.Contains("-a"))
            {
                var eb = new DiscordEmbedBuilder()
                    .WithColor(new DiscordColor(220, 152, 164))
                    .WithAuthor($"Top osu! {mode} Plays for {scores.First().Username}",
                        $"https://osu.ppy.sh/users/{scores.First().UserId}",
                        $"http://s.ppy.sh/a/{scores.First().UserId}");
                var pages = new List<Page>();
                var content = "";
                var counter = 0;
                await ctx.TriggerTypingAsync();
                foreach (var score in scores.Take(50))
                {
                    var map = await score.GetBeatmapAsync();
                    var pp = await score.GetPPAsync();
                    var scoreIfFc = "";
                    if (Utilities.IsChoke(score, map.MaxCombo))
                    {
                        var fcData = await OppaiClient.GetPPAsync(map.BeatmapId, score.Mods, (float) score.Accuracy,
                            map.MaxCombo);
                        scoreIfFc = $"({Math.Round(fcData.Pp, 2)}pp for {Math.Round(fcData.Accuracy, 2)}% FC)";
                    }

                    var ncString = score.Mods.ToString().ToLower().Contains("nightcore")
                        ? score.Mods.ToString().Replace("DoubleTime, ", "")
                        : null;
                    content +=
                        $"**#{counter + 1} [{map.Title}](https://osu.ppy.sh/b/{map.BeatmapId})** [{map.Difficulty}] +{ncString ?? score.Mods.ToString()} [{Math.Round(pp.Stars, 2)}★]\n" +
                        $"» {DiscordEmoji.FromName(ctx.Client, $":{score.Rank}_Rank:")} » **{Math.Round(score.PerformancePoints ?? pp.Pp, 2)}pp** {scoreIfFc} » {Math.Round(score.Accuracy, 2)}%\n" +
                        $"» {score.TotalScore} » {score.MaxCombo}/{map.MaxCombo} » [{score.Count300}/{score.Count100}/{score.Count50}/{score.Miss}]\n" +
                        $"» Achieved on {score.Date:dd.MM.yy H:mm:ss}\n\n";

                    if (++counter % 5 != 0) continue;
                    eb.WithDescription(content);
                    pages.Add(new Page(embed: eb));
                    content = "";
                }

                await Interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages);
            }
            //Show only one play (the nth one)
            else if (args.Contains("-p"))
            {
                var score = scores[num - 1];
                var map = await score.GetBeatmapAsync();
                var pp = await score.GetPPAsync();
                var fcData =
                    await OppaiClient.GetPPAsync(map.BeatmapId, score.Mods, (float) score.Accuracy, map.MaxCombo);
                var scoreIfFc = Utilities.IsChoke(score, map.MaxCombo)
                    ? $"({Math.Round(fcData.Pp, 2)}pp for {Math.Round(fcData.Accuracy, 2)}% FC)"
                    : "";
                var ncString = score.Mods.ToString().ToLower().Contains("nightcore")
                    ? score.Mods.ToString().Replace("DoubleTime, ", "")
                    : null;
                var eb = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Gold)
                    .WithAuthor($"#{num} osu {mode} play for {scores.First().Username}",
                        $"https://osu.ppy.sh/users/{scores.First().UserId}",
                        $"http://s.ppy.sh/a/{scores.First().UserId}")
                    .WithThumbnail(map.ThumbnailUri)
                    .WithDescription(
                        $"**[{map.Title}](https://osu.ppy.sh/b/{map.BeatmapId})** [{map.Difficulty}] +{ncString ?? score.Mods.ToString()} [{Math.Round(pp.Stars, 2)}★]\n" +
                        $"» {DiscordEmoji.FromName(ctx.Client, $":{score.Rank}_Rank:")} » **{Math.Round(score.PerformancePoints ?? pp.Pp, 2)}pp** {scoreIfFc} » {Math.Round(score.Accuracy, 2)}%\n" +
                        $"» {score.TotalScore} » {score.MaxCombo}/{map.MaxCombo} » [{score.Count300}/{score.Count100}/{score.Count50}/{score.Miss}]\n" +
                        $"» Achieved on {score.Date:dd.MM.yy H:mm:ss}\n\n");
                await ctx.RespondAsync(embed: eb.Build());
                DataStorage.GetGuild(ctx.Guild).UpdateBeatmapInChannel(ctx.Channel.Id, map.BeatmapId);
            }
            //Default, show top 5 plays
            else
            {
                var recent = false;
                var unsortedScores = scores.ToList();
                if (args.Contains("-r"))
                {
                    recent = true;
                    scores = args.Contains("-rr")
                        ? scores.OrderBy(s => s.Date).ToList()
                        : scores.OrderByDescending(s => s.Date).ToList();
                }

                var content = "";
                var counter = 0;
                foreach (var score in scores.Take(5))
                {
                    var map = await score.GetBeatmapAsync();
                    var pp = await score.GetPPAsync();
                    var scoreIfFc = "";
                    if (Utilities.IsChoke(score, map.MaxCombo))
                    {
                        var fcData = await OppaiClient.GetPPAsync(map.BeatmapId, score.Mods, (float) score.Accuracy,
                            map.MaxCombo);
                        scoreIfFc = $"({Math.Round(fcData.Pp, 2)}pp for {Math.Round(fcData.Accuracy, 2)}% FC)";
                    }

                    var ncString = score.Mods.ToString().ToLower().Contains("nightcore")
                        ? score.Mods.ToString().Replace("DoubleTime, ", "")
                        : null;

                    content +=
                        $"**#{(recent ? unsortedScores.IndexOf(score) + 1 : ++counter)} [{map.Title}](https://osu.ppy.sh/b/{map.BeatmapId})** [{map.Difficulty}] +{ncString ?? score.Mods.ToString()} [{Math.Round(pp.Stars, 2)}★]\n" +
                        $"» {DiscordEmoji.FromName(ctx.Client, $":{score.Rank}_Rank:")} » **{Math.Round(score.PerformancePoints ?? pp.Pp, 2)}pp** {scoreIfFc} » {Math.Round(score.Accuracy, 2)}%\n" +
                        $"» {score.TotalScore} » {score.MaxCombo}/{map.MaxCombo} » [{score.Count300}/{score.Count100}/{score.Count50}/{score.Miss}]\n" +
                        $"» Achieved on {score.Date:dd.MM.yy H:mm}\n\n";
                }

                var eb = new DiscordEmbedBuilder()
                    .WithDescription(content)
                    .WithColor(DiscordColor.Gold)
                    .WithAuthor($"Top osu! {mode} Plays for {scores.First().Username}",
                        $"https://osu.ppy.sh/users/{scores.First().UserId}",
                        $"http://s.ppy.sh/a/{scores.First().UserId}");
                await ctx.RespondAsync(embed: eb.Build());
            }
        }
    }
}