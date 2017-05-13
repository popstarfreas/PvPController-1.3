using TShockAPI;
using Terraria;
using System;
using Microsoft.Xna.Framework;
using System.Linq;

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

        public DateTime LastPvPEnabled
        {
            set;
            get;
        }

        public bool IsDead
        {
            set;
            get;
        }

        public bool Spectating
        {
            set;
            get;
        }

        public DateTime LastSpectating
        {
            set;
            get;
        }

        public DateTime LastHeal
        {
            set;
            get;
        }

        private PvPController Controller;

        public Player(TSPlayer player, PvPController controller)
        {
            Controller = controller;
            ProjectileWeapon = new Item[Main.maxProjectileTypes];
            TshockPlayer = player;
            LastHeal = DateTime.Now.AddSeconds(-60);
            LastSpectating = DateTime.Now.AddSeconds(-30);
            Spectating = false;
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

        /// <summary>
        /// Removes a players projectile
        /// </summary>
        /// <param name="projectileIndex">The index of the projectile</param>
        public void RemoveProjectile(int projectileIndex)
        {
            var proj = Main.projectile[projectileIndex];
            proj.active = false;
            proj.type = 0;
            TSPlayer.All.SendData(PacketTypes.ProjectileDestroy, "", projectileIndex);
        }

        /// <summary>
        /// Removes a projectile and tells the player that it does not work.
        /// </summary>
        /// <param name="hideDisallowedProjectiles">Whether or not to hide the projectile</param>
        /// <param name="projectileIndex">The index of the projectile</param>
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

        /// <summary>
        /// Ensures that this player has non-prefixed armor if the option is enabled
        /// </summary>
        /// <param name="handlers"></param>
        internal void CheckArmorAndEnforce(GetDataHandlers handlers)
        {
            if (TPlayer.armor[0].prefix > 0)
            {
                ForceItem(59, 0, TPlayer.armor[0].netID, 1);
            }

            if (TPlayer.armor[1].prefix > 0)
            {
                ForceItem(60, 0, TPlayer.armor[1].netID, 1);
            }

            if (TPlayer.armor[2].prefix > 0)
            {
                ForceItem(61, 0, TPlayer.armor[2].netID, 1);
            }
        }

        /// <summary>
        /// Forces a players active health to a given value
        /// </summary>
        /// <param name="player">The player that is being updated</param>
        /// <param name="health">The new health value</param>
        internal void ForceActiveHealth(int health)
        {
            ForceClientSSC(true);
            TPlayer.statLife = health;
            NetMessage.SendData((int)PacketTypes.PlayerHp, -1, -1, "", TshockPlayer.Index);
            ForceClientSSC(false);
        }

        /// <summary>
        /// Forces a slot to a specific item regardless of SSC
        /// </summary>
        /// <param name="player">The player that is being updated</param>
        /// <param name="slotId">The slot id of the slot to update</param>
        /// <param name="prefix">The prefix to set on the item</param>
        /// <param name="netId">The netId to set on the item</param>
        internal void ForceItem(int slotId, int prefix, int netId, int stack)
        {
            ForceClientSSC(true);
            ForceServerItem(slotId, prefix, netId, stack);
            ForceClientSSC(false);
        }

        /// <summary>
        /// Forces a slot to a specific item in the server storage of the players inventory and broadcasts the update
        /// </summary>
        /// <param name="player"></param>
        /// <param name="slotId"></param>
        /// <param name="prefix"></param>
        /// <param name="netId"></param>
        /// <param name="stack"></param>
        internal void ForceServerItem(int slotId, int prefix, int netId, int stack)
        {
            if (slotId < NetItem.InventorySlots)
            {
                //58
                TPlayer.inventory[slotId].netDefaults(netId);

                if (TPlayer.inventory[slotId].netID != 0)
                {
                    TPlayer.inventory[slotId].stack = stack;
                    TPlayer.inventory[slotId].prefix = (byte)prefix;
                    NetMessage.SendData(5, -1, -1, NetworkText.FromLiteral(TPlayer.inventory[slotId].name), TPlayer.whoAmI, slotId, TPlayer.inventory[slotId].prefix, TPlayer.inventory[slotId].stack);
                }
            }
            else if (slotId < NetItem.InventorySlots + NetItem.ArmorSlots)
            {
                //59-78
                var index = slotId - NetItem.InventorySlots;
                TPlayer.armor[index].netDefaults(netId);

                if (TPlayer.armor[index].netID != 0)
                {
                    TPlayer.armor[index].stack = stack;
                    TPlayer.armor[index].prefix = (byte)prefix;
                    NetMessage.SendData(5, -1, -1, NetworkText.FromLiteral(TPlayer.armor[index].name), TPlayer.whoAmI, slotId, TPlayer.armor[index].prefix, TPlayer.armor[index].stack);
                }
            }
            else if (slotId < NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots)
            {
                //79-88
                var index = slotId - (NetItem.InventorySlots + NetItem.ArmorSlots);
                TPlayer.dye[index].netDefaults(netId);

                if (TPlayer.dye[index].netID != 0)
                {
                    TPlayer.dye[index].stack = stack;
                    TPlayer.dye[index].prefix = (byte)prefix;
                    NetMessage.SendData(5, -1, -1, NetworkText.FromLiteral(TPlayer.dye[index].name), TPlayer.whoAmI, slotId, TPlayer.dye[index].prefix, TPlayer.dye[index].stack);
                }
            }
            else if (slotId <
                NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.MiscEquipSlots)
            {
                //89-93
                var index = slotId - (NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots);
                TPlayer.miscEquips[index].netDefaults(netId);

                if (TPlayer.miscEquips[index].netID != 0)
                {
                    TPlayer.miscEquips[index].stack = stack;
                    TPlayer.miscEquips[index].prefix = (byte)prefix;
                    NetMessage.SendData(5, -1, -1, NetworkText.FromLiteral(TPlayer.miscEquips[index].name), TPlayer.whoAmI, slotId, TPlayer.miscEquips[index].prefix, TPlayer.miscEquips[index].stack);
                }
            }
            else if (slotId <
                NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.MiscEquipSlots
                + NetItem.MiscDyeSlots)
            {
                //93-98
                var index = slotId - (NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots
                    + NetItem.MiscEquipSlots);
                TPlayer.miscDyes[index].netDefaults(netId);

                if (TPlayer.miscDyes[index].netID != 0)
                {
                    TPlayer.miscDyes[index].stack = stack;
                    TPlayer.miscDyes[index].prefix = (byte)prefix;
                    NetMessage.SendData(5, -1, -1, NetworkText.FromLiteral(TPlayer.miscDyes[index].name), TPlayer.whoAmI, slotId, TPlayer.miscDyes[index].prefix, TPlayer.miscDyes[index].stack);
                }
            }
        }

        /// <summary>
        /// Forces a clients SSC to a specific value
        /// </summary>
        /// <param name="on">Whether SSC is to be set to on or not</param>
        /// <param name="player"></param>
        internal void ForceClientSSC(bool on)
        {

            Main.ServerSideCharacter = on;
            NetMessage.SendData((int)PacketTypes.WorldInfo, TshockPlayer.Index, -1, NetworkText.FromLiteral(""));
        }

        /// <summary>
        /// Modifies a projectile based on the controller settings
        /// </summary>
        /// <param name="player">The player who owns the projectile</param>
        /// <param name="ident">The index of the projectile in the array</param>
        /// <param name="owner">The owner id of the projectile</param>
        /// <param name="type">The type of the projectile</param>
        /// <param name="dmg">The damage of the projectile (used to checking against things like hook projectiles)</param>
        /// <param name="vel">The velocity of the projectile</param>
        /// <param name="pos">The position of the projectile</param>
        /// <returns></returns>
        internal bool ModifyProjectile(int ident, int owner, int type, int dmg, Vector2 vel, Vector2 pos)
        {
            Item weaponUsed = TshockPlayer.SelectedItem;
            weaponUsed = ProjectileMapper.DetermineWeaponUsed(type, this);

            var proj = new Projectile();
            proj.SetDefaults(type);

            // Apply buffs to user if weapon buffs exist
            if (Controller.Weapons.Count(p => p.netID == weaponUsed.netID && p.buffs.Count() > 0) > 0)
            {
                if (proj.ranged && dmg > 0)
                {
                    var weapon = Controller.Weapons.FirstOrDefault(p => p.netID == TshockPlayer.SelectedItem.netID);
                    var weaponBuffs = weapon.buffs;
                    foreach (var weaponBuff in weaponBuffs)
                    {
                        TshockPlayer.SetBuff(weaponBuff.netID, Convert.ToInt32((weaponBuff.milliseconds / 1000f) * 60), true);
                    }
                }
            }

            // Load weapon modifications if this is a damaging projectile and the used weapon has modifications
            var modification = Controller.Projectiles.FirstOrDefault(p => p.netID == type);
            StorageTypes.Weapon weaponModification = null;
            if (dmg > 0)
            {
                weaponModification = Controller.Weapons.FirstOrDefault(p => p.netID == TshockPlayer.SelectedItem.netID);
            }


            // Apply modifications and update if they exist
            if ((modification != null && modification.velocityRatio != 1f) || (weaponModification != null && weaponModification.velocityRatio != 1f))
            {
                proj = Main.projectile[ident];
                var velocity = vel;
                if (modification != null)
                {
                    velocity *= modification.velocityRatio;
                }

                if (weaponModification != null)
                {
                    velocity *= weaponModification.velocityRatio;
                }
                proj.SetDefaults(type);
                proj.damage = dmg;
                proj.active = true;
                proj.identity = ident;
                proj.owner = owner;
                proj.velocity = velocity;
                proj.position = pos;
                NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, NetworkText.FromLiteral(""), ident);
                return true;
            }

            return false;
        }
    }
}
