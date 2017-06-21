using TShockAPI;
using System.Linq;
using Terraria;

namespace PvPController
{
    public class WeaponUseTimeMapper
    {
        public static int DetermineUseTime(Item weapon, Player player)
        {
            int useTime = weapon.useTime;
            switch (weapon.netID)
            {
                case 788: // Nettle Burst
                    useTime = HandleNettleBurst(weapon, player);
                    break;
                case 1308: // Poison Staff
                    useTime = HandlePoisonStaff(weapon, player);
                    break;
            }

            return useTime;
        }

        /// <summary>
        /// The use time of a poison staff is determined by real use time / projectile count
        /// </summary>
        /// <returns>The workable use time</returns>
        private static int HandleNettleBurst(Item nettleBurst, Player player)
        {
            int useTime = 0;

            if (!player.UseTimePreventActive)
            {
                player.UseTimePreventActive = true;
                player.UseTimePrevent = 12;
            }
            else
            {
                if (--player.UseTimePrevent == 0)
                {
                    useTime = nettleBurst.useTime;
                    player.UseTimePreventActive = false;
                }
            }

            return useTime;
        }

        /// <summary>
        /// The use time of a poison staff is determined by real use time / projectile count
        /// </summary>
        /// <returns>The workable use time</returns>
        private static int HandlePoisonStaff(Item poisonStaff, Player player)
        {
            int useTime = 0;

            if (!player.UseTimePreventActive)
            {
                TSPlayer.All.SendErrorMessage("Starting use time prevent...");
                player.UseTimePreventActive = true;
                player.UseTimePrevent = 2;
            } else
            {

                TSPlayer.All.SendErrorMessage($"Checking {player.UseTimePrevent-1} == 0");
                if (--player.UseTimePrevent == 0)
                {
                    useTime = poisonStaff.useTime;
                    player.UseTimePreventActive = false;
                    TSPlayer.All.SendErrorMessage($"Setting useTime to {useTime}");
                }
            }

            return useTime;
        }
    }
}
