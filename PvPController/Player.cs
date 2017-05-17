using TShockAPI;
using Terraria;
using System;
using Microsoft.Xna.Framework;
using System.Linq;
using Terraria.DataStructures;
using PvPController.Network;

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

        public PlayerKiller Killer
        {
            set;
            get;
        }

        /// <summary>
        /// Prevents use time activating if > 0
        /// </summary>
        internal int UseTimePrevent
        {
            set;
            get;
        }

        internal bool UseTimePreventActive
        {
            set;
            get;
        }

        /// <summary>
        /// The amount to allow a player to cheat by to prevent legit people being prevented
        /// </summary>
        private int UseTimeBoundary = 20;

        private PvPController Controller;

        public Player(TSPlayer player, PvPController controller)
        {
            Controller = controller;
            ProjectileWeapon = new Item[Main.maxProjectileTypes];
            TshockPlayer = player;
            LastHeal = DateTime.Now.AddSeconds(-60);
            LastSpectating = DateTime.Now.AddSeconds(-30);
            Spectating = false;
            UseTimePrevent = 0;
            UseTimePreventActive = false;
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
        /// <param name="projectileId">The index of the projectile</param>
        public void RemoveProjectile(int projectileId)
        {
            var projectileDestroy = new PacketFactory()
                .SetType((int)PacketTypes.ProjectileDestroy)
                .PackInt16((short)projectileId)
                .PackByte((byte)TPlayer.whoAmI) // Owner
                .GetByteData();

            var projectileNew = new PacketFactory()
                .SetType((int)PacketTypes.ProjectileNew)
                .PackInt16((short)projectileId)
                .PackSingle(0) // Position
                .PackSingle(0)
                .PackSingle(0) // Velocity
                .PackSingle(0)
                .PackSingle(0) // Knockback
                .PackInt16(0) // Damage
                .PackByte((byte)TPlayer.whoAmI) // Owner
                .PackInt16((short)0) // Type
                .PackByte(0) // AI
                .GetByteData();

            TshockPlayer.SendRawData(projectileDestroy);
            TshockPlayer.SendRawData(projectileNew);
        }

        /// <summary>
        /// Removes a projectile and tells the player that it does not work.
        /// </summary>
        /// <param name="hideDisallowedProjectiles">Whether or not to hide the projectile</param>
        /// <param name="projectileId">The index of the projectile</param>
        public void RemoveProjectileAndTellIsIneffective(int projectileId)
        {
            RemoveProjectile(projectileId);

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
                TPlayer.armor[0].prefix = 0;
                Controller.DataSender.SendSlotUpdate(this, 59, TPlayer.armor[0]);
            }

            if (TPlayer.armor[1].prefix > 0)
            {
                Controller.DataSender.SendSlotUpdate(this, 60, TPlayer.armor[1]);
            }

            if (TPlayer.armor[2].prefix > 0)
            {
                Controller.DataSender.SendSlotUpdate(this, 61, TPlayer.armor[2]);
            }
        }

        /// <summary>
        /// Modifies a projectile based on the controller settings
        /// </summary>
        /// <param name="args">The information about the projectile</param>
        /// <returns></returns>
        internal bool ModifyProjectile(ProjectileArgs args)
        {
            int existingIndex = ProjectileUtils.FindProjectileIndex(args.Ident, TPlayer.whoAmI);
            var proj = new Projectile();
            proj.SetDefaults(args.Type);
            
            Item weaponUsed = TshockPlayer.SelectedItem;
            weaponUsed = ProjectileMapper.DetermineWeaponUsed(args.Type, this);

            if (existingIndex > -1)
            {
                return false;
            }

            // Apply buffs to user if weapon buffs exist
            if (Controller.Weapons.Count(p => p.netID == weaponUsed.netID && p.buffs.Count() > 0) > 0)
            {
                if (proj.ranged && args.Damage > 0)
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
            var modification = Controller.Projectiles.FirstOrDefault(p => p.netID == args.Type);
            StorageTypes.Weapon weaponModification = null;
            if (args.Damage > 0)
            {
                weaponModification = Controller.Weapons.FirstOrDefault(p => p.netID == TshockPlayer.SelectedItem.netID);
            }


            // Apply modifications and update if they exist
            if ((modification != null && modification.velocityRatio != 1f) || (weaponModification != null && weaponModification.velocityRatio != 1f))
            {
                int freeIndex = ProjectileUtils.FindFreeIndex();

                if (freeIndex > -1)
                {
                    proj = Main.projectile[freeIndex];
                    var velocity = args.Velocity;
                    if (modification != null)
                    {
                        velocity *= modification.velocityRatio;
                    }

                    if (weaponModification != null)
                    {
                        velocity *= weaponModification.velocityRatio;
                    }
                    proj.SetDefaults(args.Type);
                    proj.damage = args.Damage;
                    proj.active = true;
                    proj.identity = args.Ident;
                    proj.owner = args.Owner;
                    proj.velocity = velocity;
                    proj.position = args.Position;
                    NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, NetworkText.FromLiteral(""), freeIndex);
                }

                return true;
            }

            return false;
        }



        /// <summary>
        /// Applies a player heal for the given amount, not going over the players max hp
        /// </summary>
        /// <param name="player">The player who is healing</param>
        /// <param name="healAmt">The amount they are healing for</param>
        public void Heal(int healAmt)
        {
            TPlayer.statLife += healAmt;
            if (TPlayer.statLife > TPlayer.statLifeMax2)
            {
                TPlayer.statLife = TPlayer.statLifeMax2;
            }

            Controller.DataSender.SendClientHealth(this, TPlayer.statLife);
        }

        /// <summary>
        /// Updates this players health, keeping it within the bounds of their max health
        /// </summary>
        /// <param name="health">The amount to set this players health to</param>
        public void SetActiveHealth(int health)
        {
            TPlayer.statLife = health;
            if (TPlayer.statLife > TPlayer.statLifeMax2)
            {
                TPlayer.statLife = TPlayer.statLifeMax2;
            }

            Controller.DataSender.SendClientHealth(this, TPlayer.statLife);
        }

        /// <summary>
        /// Applies an amount of damage to this player, updating their client and the server version of their health
        /// </summary>
        /// <param name="killer"></param>
        /// <param name="victim"></param>
        /// <param name="killersWeapon"></param>
        /// <param name="dir"></param>
        /// <param name="damage"></param>
        /// <param name="realDamage"></param>
        public void ApplyPlayerDamage(Player killer, Item killersWeapon, int dir, int damage, int realDamage)
        {
            // Send the damage using the special method to avoid invincibility frames issue
            Controller.DataSender.SendPlayerDamage(TshockPlayer, dir, damage);
            Controller.RaisePlayerDamageEvent(this, new PlayerDamageEventArgs(killer.TshockPlayer, TshockPlayer, killersWeapon, realDamage));
            TPlayer.statLife -= realDamage;

            // Hurt the player to prevent instant regen activating
            var savedHealth = TPlayer.statLife;
            TPlayer.Hurt(new PlayerDeathReason(), damage, 0, true, false, false, 3);
            TPlayer.statLife = savedHealth;

            if (TPlayer.statLife <= 0)
            {
                TshockPlayer.Dead = true;
                IsDead = true;
                Controller.DataSender.SendPlayerDeath(TshockPlayer);

                if (TPlayer.hostile && Killer != null)
                {
                    PlayerKillEventArgs killArgs = new PlayerKillEventArgs(Killer.Player, TshockPlayer, Killer.Weapon);
                    Controller.RaisePlayerKillEvent(this, killArgs);
                    Killer = null;
                }
                return;
            }

            Controller.DataSender.SendClientHealth(this, TPlayer.statLife);
        }
    }
}
