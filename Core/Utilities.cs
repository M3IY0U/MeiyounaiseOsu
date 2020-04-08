using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using OsuSharp;

namespace MeiyounaiseOsu.Core
{
    public class Utilities
    {
        private static readonly Dictionary<string, string> Keys;
        public static readonly Regex MapUrls = new Regex(@"https?://osu.ppy.sh/b(eatmapsets)?/[0-9]+(#[A-z]+/[0-9]+)?");


        static Utilities()
        {
            var json = File.ReadAllText("keys.json");
            Keys = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        }

        public static string GetKey(string service)
        {
            return Keys.ContainsKey(service) ? Keys[service] : "";
        }

        public static async Task ConfirmCommand(DiscordMessage msg)
            => await msg.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));

        public static async Task<List<uint>> GetTotalObjectsAsync(long id)
        {
            await DownloadAsync(new Uri($"https://osu.ppy.sh/osu/{id}"), $"{id}.txt");
            var lines = File.ReadAllLines($"{id}.txt");
            var objects = lines.SkipWhile(line => !line.Contains("HitObjects")).ToList();
            objects.RemoveAt(0);
            var timingList = objects.Select(x => x.Substring(x.IndexOf(',', x.IndexOf(',') + 1) + 1))
                .Select(time => Convert.ToUInt32(time.Remove(time.IndexOf(',')))).ToList();
            File.Delete($"{id}.txt");
            return timingList;
        }

        public static bool IsChoke(Score score, int? maxCombo)
        {
            return score.Accuracy < 95 || score.Rank == "F" || (score.Miss >= 1 || (score.MaxCombo <= 0.95 * maxCombo && score.Rank == "S"));
        }

        public static async Task DownloadAsync(Uri requestUri, string filename)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var handler = new HttpClientHandler();
            using var httpClient = new HttpClient(handler, false);
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            await using Stream contentStream = await (await httpClient.SendAsync(request)).Content.ReadAsStreamAsync(),
                stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None, 4096,
                    true);
            await contentStream.CopyToAsync(stream);
        }
        
        public static async Task<string> ToHastebin(string content)
        {
            using var client = new HttpClient();
            var response = await client.PostAsync("https://haste.timostestdoma.in/documents",
                new StringContent(content));
            var rs = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<dynamic>(rs);
            return $"https://haste.timostestdoma.in/{data.key}";
        }

    }
}