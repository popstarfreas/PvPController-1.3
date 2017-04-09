using TShockAPI;
using Terraria;
using System;
using Microsoft.Xna.Framework;

namespace PvPController
{
    public class Player
    {
        public TSPlayer TshockPlayer { private set; get; }
        private DateTime LastMessage;
        public int Index
        {
            get
            {
                return TshockPlayer.Index;
            }
        }

        public Terraria.Player TPlayer
        {
            get
            {
                return TshockPlayer.TPlayer;
            }
        }

        // Tracks The last active bow weapon for the specified player index
        public Item LastActiveBow
        {
            set;
            get;
        }

        // Tracks what weapon created what projectile for the specified projectile index
        public Item[] ProjectileWeapon
        {
            private set;
            get;
        }

        public Player(TSPlayer player)
        {
            ProjectileWeapon = new Item[Main.maxProjectileTypes];
            TshockPlayer = player;
        }

        /**
         * Tells the player that the weapon they are using does not work in pvp.
         */
        public void TellWeaponIsIneffective()
        {
            if ((DateTime.Now - LastMessage).TotalSeconds > 2)
            {
                TshockPlayer.SendMessage("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++", Color.Red);
                TshockPlayer.SendMessage("That weapon does not work in PVP. Using it will cause you to do no damage!", Color.Red);
                TshockPlayer.SendMessage("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++", Color.Red);
                LastMessage = DateTime.Now;
            }
        }

        /**
         * Removes a projectile and tells the player that it does not work.
         */
        public void RemoveProjectileAndTellIsIneffective(bool hideDisallowedProjectiles, int projectileIndex)
        {
            var proj = Main.projectile[projectileIndex];
            proj.active = false;
            proj.type = 0;
            if (hideDisallowedProjectiles)
            {
                TSPlayer.All.SendData(PacketTypes.ProjectileDestroy, "", projectileIndex);
            }
            proj.owner = 255;
            proj.active = false;
            proj.type = 0;
            TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", projectileIndex);

            if ((DateTime.Now - LastMessage).TotalSeconds > 2)
            {
                TshockPlayer.SendMessage("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++", Color.Red);
                TshockPlayer.SendMessage("That projectile does not work in PVP. Using it will cause you to do no damage!", Color.Red);
                TshockPlayer.SendMessage("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++", Color.Red);
                LastMessage = DateTime.Now;
            }
        }
    }
}
