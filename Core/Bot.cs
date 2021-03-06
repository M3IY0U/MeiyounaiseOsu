using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using MeiyounaiseOsu.Discord;
using MeiyounaiseOsu.Entities;
using Microsoft.Extensions.DependencyInjection;
using OsuSharp;

namespace MeiyounaiseOsu.Core
{
    internal class Bot : IDisposable
    {
        public static DiscordClient Client;
        private CommandsNextExtension _cnext;

        public Bot()
        {
            Client = new DiscordClient(new DiscordConfiguration
            {
                Token = Utilities.GetKey("token"),
                UseInternalLogHandler = true
            });


            var osuClient = new OsuClient(new OsuSharpConfiguration
            {
                ApiKey = Utilities.GetKey("osu"),
                ModeSeparator = " | "
            });

            Client.UseInteractivity(new InteractivityConfiguration());

            var serviceProvider = new ServiceCollection()
                .AddSingleton(osuClient)
                .BuildServiceProvider();
            

            _cnext = Client.UseCommandsNext(new CommandsNextConfiguration
            {
                EnableDms = false,
                PrefixResolver = CustomPrefixPredicate,
                Services = serviceProvider
            });

            Client.GuildCreated += Guild.ClientOnGuildCreated;
            Client.MessageCreated += Guild.LogBeatmap;
            Client.GuildDownloadCompleted += Tracking.FetchTopPlays;
            Client.SocketErrored += args =>
            {
                var ex = args.Exception;
                while (ex is AggregateException)
                    ex = ex.InnerException;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}]Socket threw an exception {ex?.GetType()}: {ex?.Message}");
                Console.ResetColor();
                return Task.CompletedTask;
            };
            
            _cnext.RegisterCommands(Assembly.GetEntryAssembly());
            var pollingTimer = new Timer
            {
                Enabled = true,
                Interval = 120000,
                AutoReset = true
            };

            pollingTimer.Elapsed += Tracking.TimerElapsed;

            _cnext.CommandErrored += async args =>
            {
                if (args.Exception.Message.Contains("command was not found"))
                    return;
                await args.Context.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("❎"));
                await args.Context.RespondAsync($"{args.Exception.Message}");
            };

            GC.KeepAlive(pollingTimer);
        }

        private static Task<int> CustomPrefixPredicate(DiscordMessage msg)
        {
            var guild = DataStorage.GetGuild(msg.Channel.Guild);
            return msg.Content.StartsWith(guild.Prefix) ? Task.FromResult(guild.Prefix.Length) : Task.FromResult(-1);
        }

        public async Task RunAsync()
        {
            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        public void Dispose()
        {
            Client.Dispose();
            _cnext = null;
        }
    }
}