using System;
using System.Linq;
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
    [Group("recent", CanInvokeWithoutSubcommand = true), Aliases("rs")]
    public class Recent
    {
        private OsuClient Client;

        public Recent(OsuClient client)
        {
            Client = client;
        }

        public async Task ExecuteGroupAsync(CommandContext ctx, string username = "")
        {
            await Exec(ctx, GameMode.Standard, username);
        }

        [Command("taiko")]
        public async Task Taiko(CommandContext ctx, string username = "")
        {
            await Exec(ctx, GameMode.Taiko, username);
        }

        [Command("ctb")]
        public async Task Ctb(CommandContext ctx, string username = "")
        {
            await Exec(ctx, GameMode.Catch, username);
        }

        [Command("mania")]
        public async Task Mania(CommandContext ctx, string username = "")
        {
            await Exec(ctx, GameMode.Mania, username);
        }

        private async Task Exec(CommandContext ctx, GameMode gm, string username)
        {
            if (string.IsNullOrEmpty(username))
                username = DataStorage.GetUser(ctx.User).OsuUsername;
            if (string.IsNullOrEmpty(username))
                throw new Exception("I have no username set for you! Set it using `osuset [name]`.");

            var recents = await Client.GetUserRecentsByUsernameAsync(username, gm);
            if (!recents.Any())
                throw new Exception($"User `{username}` has no recent plays! (in the past 24 hours)");

            var score = recents.First();

            var map = await Client.GetBeatmapByIdAsync(score.BeatmapId);

            var pData = await score.GetPPAsync();
            var fcData = await OppaiClient.GetPPAsync(map.BeatmapId, score.Mods, (float) score.Accuracy, map.MaxCombo);


            var tries = recents.TakeWhile(s => s.BeatmapId == score.BeatmapId).Count();

            var scoreIfFc = "";
            if (Utilities.IsChoke(score, map.MaxCombo))
                scoreIfFc = $"({Math.Round(fcData.Pp, 2)}pp for {Math.Round(fcData.Accuracy, 2)} FC)";

            var completion = "";
            if (score.Rank == "F")
            {
                var hits = score.Count50 + score.Count100 + score.Count300 + score.Miss;
                var objectTimes = await Utilities.GetTotalObjectsAsync(map.BeatmapId);
                var timing = Convert.ToDouble(objectTimes[objectTimes.Count - 1] - objectTimes[0]);
                var point = Convert.ToDouble(objectTimes[hits - 1] - objectTimes[0]);
                completion = $"» **Map Completion:** {Math.Round((point / timing) * 100, 2)}%";
            }

            var eb = new DiscordEmbedBuilder()
                .WithColor(ctx.Member.Color)
                .WithAuthor($"{map.Title} [{map.Difficulty}] +{score.Mods} [{Math.Round(map.DifficultyRating, 2)}★]",
                    $"https://osu.ppy.sh/b/{map.BeatmapId}", $"http://s.ppy.sh/a/{score.UserId}")
                .WithThumbnailUrl(map.ThumbnailUri)
                .WithDescription(
                    $"» {DiscordEmoji.FromName(ctx.Client, $":{score.Rank}_Rank:")} » **{Math.Round(pData.Pp, 2)}pp** {scoreIfFc} » {Math.Round(score.Accuracy, 2)}%\n" +
                    $"» {score.TotalScore} » x{score.MaxCombo}/{map.MaxCombo} » [{score.Count300}/{score.Count100}/{score.Count50}/{score.Miss}]" +
                    $"\n{completion}")
                .WithFooter(
                    $"Submitted {(score.Date.HasValue ? score.Date.Value.AddHours(1).Humanize() : "")} {(tries > 1 ? "| Try #" + tries : "")}");
            await ctx.RespondAsync(embed: eb.Build());
            DataStorage.GetGuild(ctx.Guild).UpdateBeatmapInChannel(ctx.Channel.Id, map.BeatmapId);
        }
    }
}