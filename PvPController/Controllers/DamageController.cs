using System.Collections.Generic;
using Terraria;

/// <summary>
/// Deals with controlling damage received from players
/// </summary>
namespace PvPController.Controllers
{
    public static class DamageController
    {
        public delegate double DamageHandler(Player attacker, Player victim, Item weapon, int projectileType, double damage);
        public static List<DamageHandler> Controllers = new List<DamageHandler>();

        public static double DecideDamage(Player attacker, Player victim, Item weapon, int projectileType, double damage)
        {
            foreach (var controller in Controllers)
            {
                damage = controller(attacker, victim, weapon, projectileType, damage);
            }

            return damage;
        }
    }
}
