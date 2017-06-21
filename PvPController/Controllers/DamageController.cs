using System.Collections.Generic;
using Terraria;

/// <summary>
/// Deals with controlling damage received from players
/// </summary>
namespace PvPController.Controllers
{
    public static class DamageController
    {
        public delegate int DamageHandler(Player attacker, Player victim, Item weapon, int damage);
        public static List<DamageHandler> Controllers = new List<DamageHandler>();

        public static int DecideDamage(Player attacker, Player victim, Item weapon, int damage)
        {
            foreach (var controller in Controllers)
            {
                damage = controller(attacker, victim, weapon, damage);
            }

            return damage;
        }
    }
}
