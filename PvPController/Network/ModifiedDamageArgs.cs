using Terraria;

namespace PvPController.Network
{
    internal struct ModifiedDamageArgs
    {
        public ModifiedDamageArgs(bool projectileModificationExists, bool weaponModificationExists, int sourceProjectileType, double safeDamage, Item weapon, Player player, Player victim)
        {
            ProjectileModificationExists = projectileModificationExists;
            WeaponModificationExists = weaponModificationExists;
            SourceProjectileType = sourceProjectileType;
            SafeDamage = safeDamage;
            Weapon = weapon;
            Attacker = player;
            Victim = victim;
        }

        public bool ProjectileModificationExists;
        public bool WeaponModificationExists;
        public int SourceProjectileType;
        public double SafeDamage;
        public Item Weapon;
        public Player Attacker;
        public Player Victim;
    }
}
