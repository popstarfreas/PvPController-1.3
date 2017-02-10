using System;
using Terraria;
using TShockAPI;

namespace PvPController
{
    public class PlayerKillEventArgs : EventArgs
    {
        public TSPlayer Killer { get; private set; }
        public TSPlayer Victim { get; private set; }
        public Item Weapon { get; private set; }

        public PlayerKillEventArgs(TSPlayer killer, TSPlayer victim, Item weapon)
        {
            Killer = killer;
            Victim = victim;
            Weapon = weapon;
        }
    }
}
