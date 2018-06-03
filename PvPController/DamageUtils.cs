using System;
using Terraria;
using System.Linq;

namespace PvPController
{
    public static class DamageUtils
    {
        /// <summary>
        /// WARNING: This assumes no state changes to the given player(s) since the real damage was calculated
        /// </summary>
        /// <param name="player"></param>
        /// <param name="realDamage"></param>
        /// <returns></returns>
        public static double GetInternalDamageFromRealDamage(Terraria.Player player, double realDamage)
        {
            float defenseMultiplier = Main.expertMode ? 0.75f : 0.5f;
            double outDamage = realDamage / (1 - player.endurance);
            if (player.setSolar && player.solarShields > 0)
            {
                outDamage *= 1.0 / 0.7;
            }
            else if (player.beetleDefense && player.beetleOrbs > 0)
            {
                outDamage *= 1.0/(1.0 - (0.15 * player.beetleOrbs));
            }

            if (player.defendedByPaladin)
            {
                var buffedFrom = Main.player.Where(p => p != null && p.team == player.team && p.team != 0 && p.Distance(player.Center) < 800.0 && p.hasPaladinShield && !p.dead && !p.immune).FirstOrDefault();
                if (buffedFrom != null)
                {
                    outDamage *= 1.0 / 0.75;
                    // TODO: Implement damaging other player
                }
            }

            return outDamage + (player.statDefense * defenseMultiplier);
        }

        public static double GetRealDamageFromInternalDamage(Terraria.Player player, double internalDamage)
        {
            float defenseMultiplier = Main.expertMode ? 0.75f : 0.5f;
            double outDamage = internalDamage - (player.statDefense * defenseMultiplier);
            if (player.setSolar && player.solarShields > 0)
            {
                outDamage *= 0.7;
            } else if (player.beetleDefense && player.beetleOrbs > 0)
            {
                outDamage *= (1.0 - (0.15 * player.beetleOrbs));
            }

            if (player.defendedByPaladin)
            {
                var buffedFrom = Main.player.Where(p => p != null && p.team == player.team && p.team != 0 && p.Distance(player.Center) < 800.0 && p.hasPaladinShield && !p.dead && !p.immune).FirstOrDefault();
                if (buffedFrom != null)
                {
                    outDamage *= 0.75;
                    // TODO: Implement damaging other player
                }
            }

            if (outDamage < 1) outDamage = 1.0;

            if (player.shadowDodge)
            {
                return 0.0;
            }

            return outDamage * (1 - player.endurance);
        }
    }
}
    