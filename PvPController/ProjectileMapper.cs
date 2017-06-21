using Terraria;

namespace PvPController
{
    public static class ProjectileMapper
    {
        public static Item DetermineWeaponUsed(int type, Player player)
        {
            Item weaponUsed = new Item();
            switch (type)
            {
                case 3:   // Grappling Hook
                case 32:  // Ivy Whip
                case 73:  // Dual Hook (Blue)
                case 74:  // Dual Hook (Red)
                case 165: // Web Slinger
                case 230: // Amethyst Hook
                case 231: // Topaz Hook
                case 232: // Sapphire Hook
                case 233: // Emerald Hook
                case 234: // Ruby Hook
                case 235: // Diamond Hook
                case 256: // Skeletron Hand Hook
                case 315: // Bat Hook
                case 322: // Spooky Hook
                case 331: // Candy Cane Hook
                case 332: // Christmas Hook
                case 372: // Fish Hook
                case 396: // Slime Hook
                case 403: // Minecart Hook
                case 446: // Anti-Gravity Hook
                    HandleHook(ref weaponUsed, type, player);
                    break;

                case 7: // Vilethorn (1)
                case 8: // Vilethorn (End)
                    HandleVilethorn(ref weaponUsed, type, player);
                    break;

                case 150: // Nettle Burst (1)
                case 151: // Nettle Burst (2)
                case 152: // Nettle Burst (End)
                    HandleNettleBurst(ref weaponUsed, type, player);
                    break;

                case 238: // Nimbus Rain Cloud
                case 239: // Nimbus Rain
                    HandleNimbus(ref weaponUsed, type, player);
                    break;

                case 244: // Crimson Rain Cloud
                case 245: // Crimson Rain
                    HandleCrimsonRain(ref weaponUsed, type, player);
                    break;
                    
                case 246: // Stynger
                    HandleStynger(ref weaponUsed, type, player);
                    break;

                case 250:
                case 251: // Rainbow (from Rainbow Gun)
                    HandleRainbowGun(ref weaponUsed, type, player);
                    break;
                    
                case 296: // Inferno Blast 
                    HandleInfernoBlast(ref weaponUsed, type, player);
                    break;
                    
                case 307: // Tiny Eater 
                    HandleTinyEater(ref weaponUsed, type, player);
                    break;
                
                case 344: // North Pole (secondary projectile stage)
                    HandleNorthPole(ref weaponUsed, type, player);
                    break;
                    
                case 400: // Molotov Fire (1)
                case 401: // Molotov Fire (2)
                case 402: // Molotov Fire (3)
                    HandleMolotovFire(ref weaponUsed, type, player);
                    break;
                    
                case 411: // Toxic Cloud (1)
                case 412: // Toxic Cloud (2)
                case 413: // Toxic Cloud (3)
                    HandleToxicCloud(ref weaponUsed, type, player);
                    break;

                case 493: // Crystal Vile Shard (1)
                case 494: // Crystal Vile Shard (End)
                    HandleCrystalVileShard(ref weaponUsed, type, player);
                    break;
                    
                case 522: // Crystal Charge 
                    HandleCrystalCharge(ref weaponUsed, type, player);
                    break;
                
                case 640: // Luminite Arrow (second phase)
                    player.ProjectileWeapon[type] = player.LastActiveBow;
                    break;

                default:
                    if (Utils.IsBow(player.TshockPlayer.SelectedItem))
                    {
                        player.LastActiveBow = player.TshockPlayer.SelectedItem;
                    }

                    player.ProjectileWeapon[type] = player.TshockPlayer.SelectedItem;

                    weaponUsed = player.TshockPlayer.SelectedItem;
                    break;
            }

            return weaponUsed;
        }

        private static void HandleHook(ref Item weaponUsed, int type, Player player)
        {
            weaponUsed.SetDefaults(0);
            player.ProjectileWeapon[type] = weaponUsed;
        }

        private static void HandleVilethorn(ref Item weaponUsed, int type, Player player)
        {
            weaponUsed.SetDefaults(64);
            player.ProjectileWeapon[type] = weaponUsed;
        }

        private static void HandleNettleBurst(ref Item weaponUsed, int type, Player player)
        {
            weaponUsed.SetDefaults(788);
            player.ProjectileWeapon[type] = weaponUsed;
        }

        private static void HandleNimbus(ref Item weaponUsed, int type, Player player)
        {
            weaponUsed.SetDefaults(1244);
            player.ProjectileWeapon[type] = weaponUsed;
        }

        private static void HandleCrimsonRain(ref Item weaponUsed, int type, Player player)
        {
            weaponUsed.SetDefaults(1256);
            player.ProjectileWeapon[type] = weaponUsed;
        }

        private static void HandleStynger(ref Item weaponUsed, int type, Player player)
        {
            weaponUsed.SetDefaults(1258);
            player.ProjectileWeapon[type] = weaponUsed;
        }

        private static void HandleRainbowGun(ref Item weaponUsed, int type, Player player)
        {
            weaponUsed.SetDefaults(1260);
            player.ProjectileWeapon[type] = weaponUsed;
        }

        private static void HandleInfernoBlast(ref Item weaponUsed, int type, Player player)
        {
            weaponUsed.SetDefaults(1445);
            player.ProjectileWeapon[type] = weaponUsed;
        }

        private static void HandleTinyEater(ref Item weaponUsed, int type, Player player)
        {
            weaponUsed.SetDefaults(1571);
            player.ProjectileWeapon[type] = weaponUsed;
        }

        private static void HandleNorthPole(ref Item weaponUsed, int type, Player player)
        {
            weaponUsed.SetDefaults(1947);
            player.ProjectileWeapon[type] = weaponUsed;
        }

        private static void HandleMolotovFire(ref Item weaponUsed, int type, Player player)
        {
            weaponUsed.SetDefaults(2590);
            player.ProjectileWeapon[type] = weaponUsed;
        }

        private static void HandleToxicCloud(ref Item weaponUsed, int type, Player player)
        {
            weaponUsed.SetDefaults(3105);
            player.ProjectileWeapon[type] = weaponUsed;
        }

        private static void HandleCrystalVileShard(ref Item weaponUsed, int type, Player player)
        {
            weaponUsed.SetDefaults(3051);
            player.ProjectileWeapon[type] = weaponUsed;
        }

        private static void HandleCrystalCharge(ref Item weaponUsed, int type, Player player)
        {
            weaponUsed.SetDefaults(3209);
            player.ProjectileWeapon[type] = weaponUsed;
        }
    }
}
