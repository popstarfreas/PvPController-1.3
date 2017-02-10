using TShockAPI;
using Terraria;

namespace PvPController
{
    public class PlayerKiller
    {
        public TSPlayer Player { get; protected set; }
        public Item Weapon { get; protected set; }

        public PlayerKiller(TSPlayer player, Item weapon)
        {
            Player = player;
            Weapon = weapon;
        }
    }
}
