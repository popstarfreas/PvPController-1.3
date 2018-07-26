using System.IO;
using Newtonsoft.Json;
using TShockAPI;
using System;

namespace PvPController
{
    public class Config
    {
        public int[] BannedArmorPieces;
        public int DamageDisableSeconds;
        public bool BanTeleportItems;
        public bool BanPrefixedArmor;
        public string RedisHost;
        public int PotionHealAmt;
        public int PotionHealCooldown;
        public DatabaseConfig Database;
        public bool PreventImpossibleEquipment;
        public bool UseDatabase;

        public Config(string path = null)
        {
            if (path != null)
            {
                LoadOrCreate(path);
            } else
            {
                SetDefaults();
            }
        }

        private void LoadOrCreate(string path)
        {
            if (path == null)
            {
                SetDefaults();
            }
            else if (!File.Exists(path))
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
            BanTeleportItems = fileConfig.BanTeleportItems;
            BanPrefixedArmor = fileConfig.BanPrefixedArmor;
            RedisHost = fileConfig.RedisHost;
            Database = fileConfig.Database;
            PotionHealAmt = fileConfig.PotionHealAmt;
            PotionHealCooldown = fileConfig.PotionHealCooldown;
            PreventImpossibleEquipment = fileConfig.PreventImpossibleEquipment;
            UseDatabase = fileConfig.UseDatabase;
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
            BanTeleportItems = true;
            BanPrefixedArmor = true;
            Database.Hostname = "localhost";
            Database.Port = 27017;
            Database.DBName = "pvpcontroller";
            RedisHost = "localhost";
            PotionHealAmt = 150;
            PotionHealCooldown = 60;
            PreventImpossibleEquipment = true;
            UseDatabase = false;
        }
    }
}
