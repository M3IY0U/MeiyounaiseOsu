using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MeiyounaiseOsu.Core;
using OsuSharp;
using OsuSharp.Oppai;

namespace MeiyounaiseOsu.Discord
{
    public class Map : BaseCommandModule
    {
        private OsuClient Client;

        public Map(OsuClient client)
        {
            Client = client;
        }

        [Command("mapinfo"), Aliases("mi")]
        public async Task MapInfo(CommandContext ctx, string url = "")
        {
            long bmId;
            if (!string.IsNullOrEmpty(url))
            {
                if (!Utilities.MapUrls.IsMatch(url))
                    throw new Exception("Beatmap URL was not recognized!");

                var id = Utilities.MapUrls.Match(url).Value;
                bmId = Convert.ToInt64(id.Substring(id.LastIndexOf('/') + 1));
            }
            else
            {
                var mapLog = DataStorage.GetGuild(ctx.Guild).MapLog;
                mapLog.TryGetValue(ctx.Channel.Id, out var mapId);
                if (mapId == 0)
                    throw new Exception(
                        "I don't know which map you mean, you can send one or use the recent command to refresh my memory");
                bmId = mapId;
            }

            var map = await Client.GetBeatmapByIdAsync(bmId);

            var acc95 = await map.GetPPAsync(95f);
            var acc98 = await map.GetPPAsync(98f);
            var acc99 = await map.GetPPAsync(99f);
            var accSs = await map.GetPPAsync(100f);

            var eb = new DiscordEmbedBuilder()
                    .WithAuthor($"{map.Author}'s set", map.BeatmapUri.AbsoluteUri)
                    .WithThumbnail(map.ThumbnailUri)
                    .WithColor(Utilities.MapColor(map.State))
                    .WithFooter(
                        $"Ranked/Loved {map.ApprovedDate:dd.MM.yy H:mm} » Submitted {map.SubmitDate:dd.MM.yy H:mm} » last updated {map.LastUpdate:dd.MM.yy H:mm}")
                    .WithTitle($"{map.Artist} - {map.Title} [{map.Difficulty}]")
                    .WithDescription(
                        $"» **Length:** {TimeSpan.FromSeconds(map.TotalLength.TotalSeconds):mm\\:ss} ({TimeSpan.FromSeconds(map.HitLength.TotalSeconds):mm\\:ss} Drain) » **BPM:** {map.Bpm} » **Difficulty:** {Math.Round(map.StarRating ?? 0, 2)}★\n" +
                        $"» **Max Combo:** {map.MaxCombo} » **CS:** {map.CircleSize} » **AR:** {map.ApproachRate} » **HP:** {map.HpDrain}\n\n" +
                        $"» **PP:** 95%: {Math.Round(acc95.Pp, 2)}pp » 98%: {Math.Round(acc98.Pp, 2)}pp » 99%: {Math.Round(acc99.Pp, 2)}pp » 100%: {Math.Round(accSs.Pp, 2)}pp\n" +
                        $"» **Difficulties:** {Math.Round(map.AimDifficulty ?? 0, 2)} Aim » {Math.Round(map.SpeedDifficulty ?? 0, 2)} Speed » {Math.Round(map.OverallDifficulty, 2)} Overall")
                    .AddField("Other Stats",
                        $"» {map.PlayCount} plays/{map.PassCount} passes ({Math.Round(map.PassCount.GetValueOrDefault() / (float) map.PlayCount.GetValueOrDefault() * 100, 2)}% success rate)\n" +
                        $"» {map.CircleCount} circles, {map.SliderCount} sliders, {map.SpinnerCount} spinners\n" +
                        $"» Genre: {map.Genre} » Language: {map.Language}")
                    
                ;

            await ctx.RespondAsync(embed: eb.Build());
        }
    }
}