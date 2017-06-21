using System.Collections.Generic;
using Terraria;

/// <summary>
/// Deals with controlling damage received from players
/// </summary>
namespace PvPController.Controllers
{
    public static class EquipController
    {
        public delegate bool EquipHandler(Player player, Item equip, int slotId);
        public static List<EquipHandler> Controllers = new List<EquipHandler>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        /// <param name="equip"></param>
        /// <param name="slotId"></param>
        /// <returns></returns>
        public static bool ShouldPreventEquip(Player player, Item equip, int slotId)
        {
            bool shouldPreventEquip = false;
            foreach (var controller in Controllers)
            {
                if (controller(player, equip, slotId))
                {
                    shouldPreventEquip = true;
                    break;
                }
            }

            return shouldPreventEquip;
        }
    }
}
