using System;
using Terraria;

namespace PvPController
{
    public static class DamageUtils
    {
        public static double GetInternalDamageFromRealDamage(Terraria.Player player, double realDamage)
        {
            float defenseMultiplier = Main.expertMode ? 0.75f : 0.5f;
            double outDamage = realDamage / (1 - player.endurance);
            return outDamage + (player.statDefense * defenseMultiplier);
        }

        public static double GetRealDamageFromInternalDamage(Terraria.Player player, double internalDamage)
        {
            float defenseMultiplier = Main.expertMode ? 0.75f : 0.5f;
            double outDamage = internalDamage - (player.statDefense * defenseMultiplier);

            if (outDamage < 1) outDamage = 1;

            return outDamage * (1 - player.endurance);
        }
    }
}
    