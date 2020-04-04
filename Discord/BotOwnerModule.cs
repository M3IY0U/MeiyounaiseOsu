using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MeiyounaiseOsu.Core;

namespace MeiyounaiseOsu.Discord
{
    public class BotOwnerModule : BaseCommandModule
    {
        [Command("update"), RequireOwner, Hidden]
        public async Task Update(CommandContext ctx)
        {
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("👋"));

            using (var exeProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "pull",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }))
            {
                exeProcess?.WaitForExit();
                var output = exeProcess?.StandardOutput.ReadToEnd();
                var error = exeProcess?.StandardError.ReadToEnd();

                if (!string.IsNullOrEmpty(output))
                    await ctx.RespondAsync($"Output:```\n{output}\n```");
                if (!string.IsNullOrEmpty(error))
                    await ctx.RespondAsync($"Error:```\n{error}\n```");
            }

            File.WriteAllText("update.txt", $"{ctx.Channel.Id}\n{DateTime.Now}");

            await Bot.Client.DisconnectAsync();

            await Task.Delay(5000);
            Process.Start("dotnet", "run");

            Environment.Exit(0);
        }

        [Command("run"), Aliases("execute", "exec"), RequireOwner, Hidden]
        public async Task Run(CommandContext ctx, string cmd, [RemainingText] string arguments = "")
        {
            await ctx.TriggerTypingAsync();
            var exeProcess = Process.Start(new ProcessStartInfo
            {
                FileName = cmd,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            var timer = new Timer {Enabled = true, Interval = 10000, AutoReset = false};
            timer.Elapsed += (sender, args) =>
            {
                exeProcess?.Kill();
                ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🛑"));
                timer.Dispose();
            };

            exeProcess?.WaitForExit();

            var output = exeProcess?.StandardOutput.ReadToEnd();
            var error = exeProcess?.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(output))
            {
                if (output.Length > 2000)
                    await ctx.RespondAsync($"Output: {await Utilities.ToHastebin(output)}");
                else
                    await ctx.RespondAsync($"Output:```\n{output}\n```");
            }

            if (!string.IsNullOrEmpty(error))
                if (error.Length > 2000)
                    await ctx.RespondAsync($"Error: {await Utilities.ToHastebin(error)}");
                else
                    await ctx.RespondAsync($"Error:```\n{error}\n```");
        }
    }
}