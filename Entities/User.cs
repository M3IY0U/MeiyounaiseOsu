using OsuSharp;

namespace MeiyounaiseOsu.Entities
{
    public class User
    {
        public string OsuUsername { get; set; }
        public ulong Id { get; set; }
        public long Rank { get; set; }
        public double Pp { get; set; }
        public GameMode DefaultMode { get; set; }

        public void UpdateRank(long playerRank, double playerPerformancePoints)
        {
            Rank = playerRank;
            Pp = playerPerformancePoints;
        }
    }
}