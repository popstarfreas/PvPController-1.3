using System.IO;
using Newtonsoft.Json;

namespace AntiPvPWeapons
{
    public class Config
    {
        public int[] BannedItemIDs;
        public int[] BannedProjectileIDs;
        public int DamageDisableSeconds;

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
            Conf.BannedItemIDs = new int[]{3063, 3065, 3570, 3571, 3542, 3473, 3389};
            Conf.BannedProjectileIDs = new int[] {};
            Conf.DamageDisableSeconds = 12;
            Conf.Write(file);
        }
    }
}
