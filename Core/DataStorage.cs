using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DSharpPlus.Entities;
using MeiyounaiseOsu.Entities;
using Newtonsoft.Json;
using OsuSharp;
using User = MeiyounaiseOsu.Entities.User;

namespace MeiyounaiseOsu.Core
{
    public static class DataStorage
    {
        private static List<User> Users;
        internal static List<Guild> Guilds;

        static DataStorage()
        {
            try
            {
                Users = JsonConvert.DeserializeObject<List<User>>(File.ReadAllText("users.json"));
            }
            catch (Exception)
            {
                Users = new List<User>();
            }

            try
            {
                Guilds = JsonConvert.DeserializeObject<List<Guild>>(File.ReadAllText("guilds.json"));
            }
            catch (Exception)
            {
                Guilds = new List<Guild>();
            }
        }

        public static void CreateUser(DiscordUser user, string username)
        {
            Users.Add(new User
            {
                OsuUsername = username,
                Id = user.Id,
                DefaultMode = GameMode.Standard
            });
            SaveUsers();
        }

        public static void CreateUser(string username)
        {
            Users.Add(new User
            {
                OsuUsername = username,
                Id = 0,
                DefaultMode = GameMode.Standard
            });
        }

        public static void CreateGuild(DiscordGuild guild)
        {
            Guilds.Add(new Guild
            {
                Id = guild.Id,
                Prefix = "<",
                OsuChannel = 0,
                TrackedUsers = new List<string>()
            });
            SaveGuilds();
        }

        public static User GetUser(DiscordUser user)
        {
            var result = from a in Users where a.Id == user.Id select a;
            return result.FirstOrDefault();
        }

        public static User GetUser(string user)
        {
            var result = from a in Users where a.OsuUsername == user select a;
            return result.FirstOrDefault();
        }

        public static Guild GetGuild(DiscordGuild guild)
        {
            var result = from a in Guilds where a.Id == guild.Id select a;
            var g = result.FirstOrDefault();
            if (g != null) return g;
            CreateGuild(guild);
            return GetGuild(guild);
        }

        public static void SaveUsers()
            => File.WriteAllText("users.json", JsonConvert.SerializeObject(Users, Formatting.Indented));


        public static void SaveGuilds()
            => File.WriteAllText("guilds.json", JsonConvert.SerializeObject(Guilds, Formatting.Indented));
    }
}