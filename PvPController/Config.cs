using System.IO;
using Newtonsoft.Json;

namespace PvPController
{
    public class Config
    {
        public int[] BannedItemIDs;
        public int[] BannedProjectileIDs;
        public int[] BannedArmorPieces;
        public int[] BannedAccessories;
        public int DamageDisableSeconds;
        public bool HideDisallowedProjectiles;
        public ConfigWeaponBuff[] WeaponBuff;
        public ConfigProjectileDamage[] ProjectileModification;
        public ConfigWeaponDamage[] WeaponModification;

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
            Conf.BannedItemIDs = new int[] { 3063, 3065, 3570, 3571, 3542, 3473, 3389 };
            Conf.BannedProjectileIDs = new int[] { };
            Conf.BannedArmorPieces = new int[] { };
            Conf.BannedAccessories = new int[] { };
            Conf.DamageDisableSeconds = 12;
            Conf.HideDisallowedProjectiles = true;

            var defaultWeaponBuff = new ConfigWeaponBuff();
            defaultWeaponBuff.weaponID = 1254;
            defaultWeaponBuff.immobiliseMilliseconds = 1000;
            defaultWeaponBuff.debuffID = 149;

            var defaultProjectileModification = new ConfigProjectileDamage();
            defaultProjectileModification.projectileID = 260;
            defaultProjectileModification.damageRatio = 2f;

            var defaultWeaponModification = new ConfigWeaponDamage();
            defaultWeaponModification.weaponID = 1827;
            defaultWeaponModification.damageRatio = 8f;

            Conf.WeaponBuff = new ConfigWeaponBuff[] { defaultWeaponBuff };
            Conf.ProjectileModification = new ConfigProjectileDamage[] { defaultProjectileModification };
            Conf.WeaponModification = new ConfigWeaponDamage[] { defaultWeaponModification };
            Conf.Write(file);   
        }
    }
}
