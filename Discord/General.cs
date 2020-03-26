using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
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

        [Command("usercompare"), Aliases("uc", "usercomp")]
        public async Task UserCompare(CommandContext ctx, string otherAccount, string ownAccount = "")
        {
            if (string.IsNullOrEmpty(ownAccount))
                ownAccount = DataStorage.GetUser(ctx.User).OsuUsername;
            if (string.IsNullOrEmpty(ownAccount))
                throw new Exception("I have no username set for you! Set it using `osuset [name]`.");
            var user1 = await Client.GetUserByUsernameAsync(ownAccount, GameMode.Standard);
            var user2 = await Client.GetUserByUsernameAsync(otherAccount, GameMode.Standard);

            if (user1 == null || user2 == null)
                throw new Exception("One or more users not found!");

            var user1String = string.Join("\n", $"» Rank: #{user1.Rank}",
                $"» Level: {Math.Round(user1.Level, 2)}", $"» PP: {Math.Round(user1.PerformancePoints, 2)}",
                $"» Accuracy: {Math.Round(user1.Accuracy, 2)}",
                $"» PlayCount: {user1.PlayCount}");

            var user2String = string.Join("\n", $"» Rank: #{user2.Rank}",
                $"» Level: {Math.Round(user2.Level, 2)}", $"» PP: {Math.Round(user2.PerformancePoints, 2)}",
                $"» Accuracy: {Math.Round(user2.Accuracy, 2)}",
                $"» PlayCount: {user2.PlayCount}");

            var eb = new DiscordEmbedBuilder()
                .WithAuthor($"Comparing {user1.Username} with {user2.Username}",
                    icon_url: "https://upload.wikimedia.org/wikipedia/commons/d/d3/Osu%21Logo_%282015%29.png")
                .WithColor(ctx.Member.Color)
                .AddField(
                    DiscordEmoji.FromName(Bot.Client, $":flag_{user1.Country.TwoLetterISORegionName.ToLower()}:") +
                    " " + user1.Username, user1String, true)
                .AddField(
                    DiscordEmoji.FromName(Bot.Client, $":flag_{user2.Country.TwoLetterISORegionName.ToLower()}:") +
                    " " + user2.Username, user2String, true)
                .WithDescription(
                    $"[{user1.Username}](https://osu.ppy.sh/users/{user1.UserId}) is {Math.Round(Math.Abs(user1.PerformancePoints - user2.PerformancePoints))}pp ({Math.Abs(user1.Rank - user2.Rank)} Ranks) {(user1.PerformancePoints > user2.PerformancePoints ? "ahead" : "behind")} of [{user2.Username}](https://osu.ppy.sh/users/{user2.UserId})");
            await ctx.RespondAsync(embed: eb.Build());
        }
    }
}