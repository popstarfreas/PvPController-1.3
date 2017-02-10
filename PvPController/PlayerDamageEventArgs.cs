using System;
using TShockAPI;
using Terraria;

namespace PvPController
{
    public class PlayerDamageEventArgs : EventArgs
    {
        public TSPlayer Killer { get; private set; }
        public TSPlayer Victim { get; private set; }
        public Item Weapon { get; private set; }
        public int Damage { get; private set; }

        public PlayerDamageEventArgs(TSPlayer killer, TSPlayer victim, Item weapon, int damage)
        {
            Killer = killer;
            Victim = victim;
            Weapon = weapon;
            Damage = damage;
        }
    }
}
