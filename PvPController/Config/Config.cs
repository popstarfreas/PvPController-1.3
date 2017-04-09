using System.IO;
using Newtonsoft.Json;

namespace PvPController
{
    public class Config
    {
        public int[] BannedArmorPieces;
        public int DamageDisableSeconds;
        public bool HideDisallowedProjectiles;
        public bool BanTeleportItems;
        public string redisHost;
        public DatabaseConfig database;

        public Config(string path)
        {
            LoadOrCreate(path);
        }

        private void LoadOrCreate(string path)
        {
            if (!File.Exists(path))
            {
                SetDefaults();
                Write(path);
            }
            else
            {
                Load(path);
            }
        }

        private void Load(string path)
        {
            var fileConfig = JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
            BannedArmorPieces = fileConfig.BannedArmorPieces;
            DamageDisableSeconds = fileConfig.DamageDisableSeconds;
            HideDisallowedProjectiles = fileConfig.HideDisallowedProjectiles;
            BanTeleportItems = fileConfig.BanTeleportItems;
            redisHost = fileConfig.redisHost;
            database = fileConfig.database;

        }

        public void Write(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public void Reload(string path)
        {
            LoadOrCreate(path);
        }

        public void SetDefaults()
        {
            BannedArmorPieces = new int[] { };
            DamageDisableSeconds = 12;
            HideDisallowedProjectiles = true;
            BanTeleportItems = true;
            database.Hostname = "localhost";
            database.Port = 27017;
            database.DBName = "pvpcontroller";
            redisHost = "localhost";
        }
    }
}
