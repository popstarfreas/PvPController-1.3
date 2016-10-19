using System.IO;
using Newtonsoft.Json;

namespace PvPController
{
    public class Config
    {
        public int DamageDisableSeconds;
        public bool HideDisallowedProjectiles;
        public string RedisHost;
        public DatabaseConfig Database;

        public void Write(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static Config Read(string path)
        {
            if (!File.Exists(path))
            {
                Config.WriteTemplates(path);
            }
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
        }

        public static void WriteTemplates(string file)
        {
            var Conf = new Config();
            Conf.DamageDisableSeconds = 12;
            Conf.HideDisallowedProjectiles = true;
            Conf.Database.Hostname = "localhost";
            Conf.Database.Port = 27017;
            Conf.Database.DBName = "pvpcontroller";
            Conf.RedisHost = "localhost";
            Conf.Write(file);   
        }
    }
}
