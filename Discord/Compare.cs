using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Humanizer;
using MeiyounaiseOsu.Core;
using OsuSharp;
using OsuSharp.Oppai;

namespace MeiyounaiseOsu.Discord
{
    public class Compare : BaseCommandModule
    {
        private OsuClient Client;

        public Compare(OsuClient client)
        {
            Client = client;
        }

        [Command("compare"), Aliases("c")]
        public async Task CompareScore(CommandContext ctx, string username = "")
        {
            var x = DataStorage.GetGuild(ctx.Guild).MapLog;
            x.TryGetValue(ctx.Channel.Id, out var value);
            if (value == 0)
                throw new Exception(
                    "I don't know which map you mean, you can send one or use the recent command to refresh my memory");
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

            var scores = await Client.GetScoresByBeatmapIdAndUsernameAsync(value, username, GameMode.Standard);

            if (scores.Count == 0 || scores == null)
                throw new Exception($"User {username} was not found or has no scores on the most recent map!");

            var map = await Client.GetBeatmapByIdAsync(value);
            var desc = "";
            long userId = 0;
            foreach (var score in scores)
            {
                userId = score.UserId;

                var scoreIfFc = "";
                var fcData = await OppaiClient.GetPPAsync(map.BeatmapId, score.Mods, (float) score.Accuracy,
                    map.MaxCombo);
                if (Utilities.IsChoke(score, map.MaxCombo))
                {
                    scoreIfFc = $"({Math.Round(fcData.Pp, 2)}pp for {Math.Round(fcData.Accuracy, 2)} FC)";
                }

                desc += $"■ **`{score.Mods}` Score** [{Math.Round(fcData.Stars, 2)}★]\n".Replace("None",
                            "No Mod") +
                        $"» {DiscordEmoji.FromName(ctx.Client, $":{score.Rank}_Rank:")} » **{Math.Round(score.PerformancePoints ?? 0.0, 2)}** {scoreIfFc} » {Math.Round(score.Accuracy, 2)}%\n" +
                        $"» {score.TotalScore} » x{score.MaxCombo}/{map.MaxCombo} » [{score.Count300}/{score.Count100}/{score.Count50}/{score.Miss}]\n" +
                        $"» Score set {score.Date.Humanize()}\n";
            }

            var eb = new DiscordEmbedBuilder()
                .WithAuthor($"Top osu! Standard Plays for {username} on {map.Title} [{map.Difficulty}]",
                    $"https://osu.ppy.sh/users/{userId}", $"http://s.ppy.sh/a/{userId}")
                .WithDescription($"{desc}\n» **[Map Link]({map.BeatmapUri})**")
                .WithThumbnailUrl(map.ThumbnailUri)
                .WithColor(ctx.Member.Color);
            await ctx.RespondAsync(embed: eb.Build());
        }
    }
}