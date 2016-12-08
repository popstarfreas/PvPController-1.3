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
                    
                case 522: // Crystal Charge 
                    HandleCrystalCharge(ref weaponUsed, type, player);
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
            weaponUsed = (new Item());
            weaponUsed.SetDefaults(1244);
            PvPController.ProjectileWeapon[player.Index, type] = weaponUsed;
        }

        public static void HandleCrimsonRain(ref Item weaponUsed, int type, TSPlayer player)
        {
            weaponUsed = (new Item());
            weaponUsed.SetDefaults(1256);
            PvPController.ProjectileWeapon[player.Index, type] = weaponUsed;
        }

        public static void HandleStynger(ref Item weaponUsed, int type, TSPlayer player)
        {
            weaponUsed = (new Item());
            weaponUsed.SetDefaults(1258);
            PvPController.ProjectileWeapon[player.Index, type] = weaponUsed;
        }

        public static void HandleRainbowGun(ref Item weaponUsed, int type, TSPlayer player)
        {
            weaponUsed = (new Item());
            weaponUsed.SetDefaults(1260);
            PvPController.ProjectileWeapon[player.Index, type] = weaponUsed;
        }
        
        public static void HandleInfernoBlast(ref Item weaponUsed, int type, TSPlayer player)
        {
            weaponUsed = (new Item());
            weaponUsed.SetDefaults(1445);
            PvPController.ProjectileWeapon[player.Index, type] = weaponUsed;
        }
        
         public static void HandleTinyEater(ref Item weaponUsed, int type, TSPlayer player)
        {
            weaponUsed = (new Item());
            weaponUsed.SetDefaults(1571);
            PvPController.ProjectileWeapon[player.Index, type] = weaponUsed;
        }
        
         public static void HandleNorthPole(ref Item weaponUsed, int type, TSPlayer player)
        {
            weaponUsed = (new Item());
            weaponUsed.SetDefaults(1947);
            PvPController.ProjectileWeapon[player.Index, type] = weaponUsed;
        }
        
         public static void HandleMolotovFire(ref Item weaponUsed, int type, TSPlayer player)
        {
            weaponUsed = (new Item());
            weaponUsed.SetDefaults(2590);
            PvPController.ProjectileWeapon[player.Index, type] = weaponUsed;
        }
        
        public static void HandleToxicCloud(ref Item weaponUsed, int type, TSPlayer player)
        {
            weaponUsed = (new Item());
            weaponUsed.SetDefaults(3105);
            PvPController.ProjectileWeapon[player.Index, type] = weaponUsed;
        }
        
        public static void HandleCrystalCharge(ref Item weaponUsed, int type, TSPlayer player)
        {
            weaponUsed = (new Item());
            weaponUsed.SetDefaults(3209);
            PvPController.ProjectileWeapon[player.Index, type] = weaponUsed;
        }
    }
}
