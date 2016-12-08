using Terraria;
using TShockAPI;

namespace PvPController
{
    public static class ProjectileMapper
    {
        public static Item DetermineWeaponUsed(int type, TSPlayer player)
        {
            Item weaponUsed = player.SelectedItem;
            switch (type)
            {
                case 238: // Nimbus Rain Cloud
                case 239: // Nimbus Rain
                    HandleNimbus(ref weaponUsed, type, player);
                    break;

                case 244: // Crimson Rain Cloud
                case 245: // Crimson Rain
                    HandleCrimsonRain(ref weaponUsed, type, player);
                    break;

                case 250:
                case 251: // Rainbow (from Rainbow Gun)
                    HandleRainbowGun(ref weaponUsed, type, player);
                    break;

                case 640: // Luminite Arrow (second phase)
                    PvPController.ProjectileWeapon[player.Index, type] = PvPController.LastActiveBow[player.Index];
                    break;

                default:
                    if (Utils.IsBow(player.SelectedItem))
                    {
                        PvPController.LastActiveBow[player.Index] = player.SelectedItem;
                    }
                    PvPController.ProjectileWeapon[player.Index, type] = player.SelectedItem;
                    break;
            }

            return weaponUsed;
        }

        public static void HandleNimbus(ref Item weaponUsed, int type, TSPlayer player)
        {
            weaponUsed = new Item();
            weaponUsed.SetDefaults(1244);
            PvPController.ProjectileWeapon[player.Index, type] = weaponUsed;
        }

        public static void HandleCrimsonRain(ref Item weaponUsed, int type, TSPlayer player)
        {
            weaponUsed = new Item();
            weaponUsed.SetDefaults(1256);
            PvPController.ProjectileWeapon[player.Index, type] = weaponUsed;
        }

        public static void HandleRainbowGun(ref Item weaponUsed, int type, TSPlayer player)
        {
            weaponUsed = new Item();
            weaponUsed.SetDefaults(1260);
            PvPController.ProjectileWeapon[player.Index, type] = weaponUsed;
        }
    }
}
