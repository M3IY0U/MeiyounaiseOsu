using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.EventArgs;
using MeiyounaiseOsu.Core;
using Newtonsoft.Json;
using OsuSharp;

namespace MeiyounaiseOsu.Entities
{
    public class Guild
    {
        public ulong Id { get; set; }
        public ulong OsuChannel { get; set; }
        public string Prefix { get; set; }
        public List<string> TrackedUsers { get; set; }
        [JsonIgnore] 
        public ConcurrentDictionary<ulong, long> MapLog = new ConcurrentDictionary<ulong, long>();
        [JsonIgnore]
        public ConcurrentDictionary<string, List<Score>> TopPlays = new ConcurrentDictionary<string, List<Score>>();

        public static Task ClientOnGuildCreated(GuildCreateEventArgs e)
        {
            DataStorage.CreateGuild(e.Guild);
            return Task.CompletedTask;
        }

        public void UpdateBeatmapInChannel(ulong channel, long beatmapId)
        {
            if (!MapLog.ContainsKey(channel))
                MapLog.TryAdd(channel, beatmapId);
            else
                MapLog.TryUpdate(channel, beatmapId, MapLog[channel]);
        }

        public static Task LogBeatmap(MessageCreateEventArgs e)
        {
            if (!Utilities.MapUrls.IsMatch(e.Message.Content)) return Task.CompletedTask;
            var id = Utilities.MapUrls.Match(e.Message.Content).Value;
            DataStorage.GetGuild(e.Guild)
                .UpdateBeatmapInChannel(e.Channel.Id, Convert.ToInt64(id.Substring(id.LastIndexOf('/') + 1)));
            Console.WriteLine($"Matched url link! {id}");
            return Task.CompletedTask;
        }
    }
}