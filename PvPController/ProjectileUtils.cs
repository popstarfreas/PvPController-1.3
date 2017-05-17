using Terraria;

namespace PvPController
{
    public class ProjectileUtils
    {
        /// <summary>
        /// Finds a projectiles index with the given identity and owner
        /// </summary>
        /// <param name="ident">The projectile identity</param>
        /// <param name="owner">The projectile owner</param>
        /// <returns>The index or -1 for no such projectile</returns>
        public static int FindProjectileIndex(int ident, int owner)
        {
            int index = -1;
            for (int i = 0; i < 1000; i++)
            {
                if (Main.projectile[i].owner == owner && Main.projectile[i].identity == ident && Main.projectile[i].active)
                {
                    index = i;
                    break;
                }
            }

            return index;
        }

        /// <summary>
        /// Finds a free projectile index
        /// </summary>
        /// <returns>A free index or -1 for no free index available</returns>
        public static int FindFreeIndex()
        {
            int freeIndex = -1;

            for (int i = 0; i < 1000; i++)
            {
                if (!Main.projectile[i].active)
                {
                    freeIndex = i;
                    break;
                }
            }

            return freeIndex;
        }
    }
}
