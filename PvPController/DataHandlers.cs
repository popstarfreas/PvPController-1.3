using System;
using System.IO;
using System.IO.Streams;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using TShockAPI;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;

namespace PvPController
{
    internal delegate bool GetDataHandlerDelegate(GetDataHandlerArgs args);

    internal class GetDataHandlerArgs : EventArgs
    {
        public Player Player { get; private set; }
        public MemoryStream Data { get; private set; }

        public GetDataHandlerArgs(Player player, MemoryStream data)
        {
            Player = player;
            Data = data;
        }
    }

    /* Contains the handlers for certain packets received by the server */
    internal class GetDataHandlers
    {
        private PlayerKiller[] Killers = new PlayerKiller[255];
        private static Dictionary<PacketTypes, GetDataHandlerDelegate> _getDataHandlerDelegates;
        private PvPController Controller;


        /* Adds the handler functions as handlers for the given PacketType */
        public GetDataHandlers(PvPController controller)
        {
            this.Controller = controller;

            _getDataHandlerDelegates = new Dictionary<PacketTypes, GetDataHandlerDelegate>
            {
                { PacketTypes.ProjectileNew, HandleProjectile },
                { PacketTypes.PlayerHurtV2, HandleDamage },
                { PacketTypes.PlayerDeathV2, HandleDeath },
                { PacketTypes.PlayerUpdate, HandlePlayerUpdate},
                { PacketTypes.PlayerHp, HandlePlayerHp },
                { PacketTypes.Teleport, HandlePlayerTeleport },
                { PacketTypes.TeleportationPotion, HandlePlayerTeleportPotion },
                { PacketTypes.PlayerSpawn, HandlePlayerSpawn },
                { PacketTypes.PlayerBuff, HandlePlayerBuffs },
                { PacketTypes.PlayerSlot, HandleInventoryUpdate },
                { PacketTypes.EffectHeal, HandleEffectHeal }
            };
        }

        /// <summary>
        /// Checks if there is a handler for the given packet type and will return whether or not
        /// the packet was handled
        /// </summary>
        /// <param name="type">The packet type</param>
        /// <param name="player">THe player that sent the packet</param>
        /// <param name="data">The packet data</param>
        /// <returns>Whether or not the packet was handled (and should therefore not be processed
        /// by anything else)</returns>
        public bool HandlerGetData(PacketTypes type, Player player, MemoryStream data)
        {
            GetDataHandlerDelegate handler;
            if (_getDataHandlerDelegates.TryGetValue(type, out handler))
            {
                try
                {
                    return handler(new GetDataHandlerArgs(player, data));
                }
                catch (Exception ex)
                {
                    TShock.Log.Error(ex.ToString());
                }
            }
            return false;
        }

        /// <summary>
        /// Applies modifications to a new projectile and removes disallowed ones
        /// </summary>
        /// <param name="args">The GetDataHandlerArgs object containing the player that sent the
        /// packet and the data of the packet</param>
        /// <returns>Whether or not the packet was handled (and should therefore not be processed
        /// by anything else)</returns>
        private bool HandleProjectile(GetDataHandlerArgs args)
        {
            var ident = args.Data.ReadInt16();
            var pos = new Vector2(args.Data.ReadSingle(), args.Data.ReadSingle());
            var vel = new Vector2(args.Data.ReadSingle(), args.Data.ReadSingle());
            var knockback = args.Data.ReadSingle();
            var dmg = args.Data.ReadInt16();
            var owner = args.Data.ReadInt8();
            var type = args.Data.ReadInt16();
            var bits = (BitsByte)args.Data.ReadInt8();
            owner = (byte)args.Player.Index;
            float[] ai = new float[Projectile.maxAI];

            bool handled = false;
            if (args.Player.TPlayer.hostile)
            {
                if (Controller.Projectiles.Count(p => p.netID == type && p.banned) > 0)
                {
                    args.Player.RemoveProjectileAndTellIsIneffective(Controller.Config.HideDisallowedProjectiles, ident);
                }
                else
                {
                    if (args.Player.IsDead)
                    {
                        handled = true;
                        args.Player.RemoveProjectile(ident);
                    }
                    else
                    {
                        handled = args.Player.ModifyProjectile(ident, owner, type, dmg, vel, pos);
                    }
                }
            }

            return handled;
        }

        /// <summary>
        /// Handles the case when the player uses a banned item and warns
        /// the player that the weapon does no damage
        /// </summary>
        /// <param name="args">The GetDataHandlers args containing the player that sent
        /// the packet and the data of the packet</param>
        /// <returns>Whether or not the packet was handled (and should therefore not be processed
        /// by anything else)</returns>
        private bool HandlePlayerUpdate(GetDataHandlerArgs args)
        {
            byte plr = args.Data.ReadInt8();
            BitsByte control = args.Data.ReadInt8();
            BitsByte pulley = args.Data.ReadInt8();
            byte item = args.Data.ReadInt8();
            var pos = new Vector2(args.Data.ReadSingle(), args.Data.ReadSingle());
            var vel = Vector2.Zero;
            
            if (control[5] && Controller.Weapons.Count(p => p.netID == args.Player.TshockPlayer.SelectedItem.netID && p.banned) > 0 && args.Player.TPlayer.hostile)
            {
                args.Player.TellWeaponIsIneffective();
            }

            bool handled = false;

            if (args.Player.IsDead)
            {
                handled = true;
            }

            return handled;
        }

        /// <summary>
        /// Handles when a player tries to update their HP, which will be
        /// rejected if they are in pvp as HP is controlled by the server only
        /// </summary>
        /// <param name="args">The GetDataHandlers args containing the player that sent
        /// the packet and the data of the packet</param>
        /// <returns>Whether or not the player was permitted a HP update</returns>
        private bool HandlePlayerHp(GetDataHandlerArgs args)
        {
            bool handled = false;
            args.Data.ReadByte();
            int newHealth = args.Data.ReadInt16();
            if (args.Player.TshockPlayer.TPlayer.hostile)
            {
                handled = true;
            }

            return handled;
        }

        /// <summary>
        /// Raises an event about who and with what weapon a player was killed.
        /// Also keeps track of when someone has died to be checked when the "spawn" themselves.
        /// </summary>
        /// <param name="args">The GetDataHandlersArgs object containing the player who sent the
        /// packet and the data in it</param>
        /// <returns>Whether or not the packet was handled (and should therefore not be processed
        /// by anything else)</returns>
        private bool HandleDeath(GetDataHandlerArgs args)
        {
            try
            {
                args.Player.IsDead = true;
            }
            catch (Exception e)
            {
                TShock.Log.ConsoleError(e.ToString());
            }

            return false;
        }

        /// <summary>
        /// Determines whether to block damage if they have recently used a banned item or modifies damage
        /// if the projectile is on the modifications list.
        /// </summary>
        /// <param name="args">The GetDataHandlerArgs object containing the player who sent the packet and the
        /// data in it</param>
        /// <returns>Whether or not the packet was handled (and should therefore not be processed
        /// by anything else</returns>
        private bool HandleDamage(GetDataHandlerArgs args)
        {
            if (args.Player == null) return false;
            var index = args.Player.Index;
            var playerId = (byte)args.Data.ReadByte();
            var damageSourceFlags = (BitsByte)args.Data.ReadByte();

            var sourceItemType = -1;
            var sourceProjectileType = -1;
            var sourceItemPrefix = -1;
            if (damageSourceFlags[0])
            {
                var sourcePlayerIndex = args.Data.ReadInt16();
            }
            if (damageSourceFlags[1])
            {
                var sourceNPCIndex = args.Data.ReadInt16();
            }
            if (damageSourceFlags[2])
            {
                var sourceProjectileIndex = args.Data.ReadInt16();
            }
            if (damageSourceFlags[3])
            {
                var sourceOtherIndex = args.Data.ReadByte();
            }
            if (damageSourceFlags[4])
            {
                sourceProjectileType = args.Data.ReadInt16();
            }
            if (damageSourceFlags[5])
            {
                sourceItemType = args.Data.ReadInt16();
            }
            if (damageSourceFlags[6])
            {
                sourceItemPrefix = args.Data.ReadByte();
            }
            
            var damage = args.Data.ReadInt16();
            var dir = args.Data.ReadByte();
            var flags = args.Data.ReadByte();
            args.Data.ReadByte();

            if (TShock.Players[playerId] == null || sourceItemType == -1 || args.Player.IsDead || Controller.Players[playerId].IsDead)
                return true;

            // The sourceItemType is only reliable if no projectile is involved
            // as sourceItemType is simply the active slot item
            Item weapon = new Item();
            if (sourceProjectileType == -1)
            {
                weapon.SetDefaults(sourceItemType);
                weapon.prefix = (byte)sourceItemPrefix;
                weapon.owner = args.Player.Index;
            }
            else if (args.Player.ProjectileWeapon[sourceProjectileType] != null)
            {
                Item bestWeaponGuess = args.Player.ProjectileWeapon[sourceProjectileType];
                weapon.SetDefaults(bestWeaponGuess.netID);
                weapon.prefix = (byte)bestWeaponGuess.prefix;
                weapon.owner = args.Player.Index;
            }

            float safeDamage = Main.player[args.Player.Index].GetWeaponDamage(weapon);
            Color msgColor;
            int realDamage;

            var proj = new Projectile();
            proj.SetDefaults(sourceProjectileType);

            // Check whether the source of damage is banned
            if (Controller.Weapons.Count(p => p.netID == weapon.netID && p.banned) > 0 || Controller.Projectiles.Count(p => p.netID == sourceProjectileType && p.banned) > 0)
            {
                args.Player.TshockPlayer.SendData(PacketTypes.PlayerHp, "", playerId);
                return true;
            }
            else
            {
                // Get the weapon and whether a modification exists
                var weaponModificationExists = Controller.Weapons.Count(p => p.netID == weapon.netID && p.damageRatio != 1f) > 0;
                var projectileModificationExists = Controller.Projectiles.Count(p => p.netID == sourceProjectileType && p.damageRatio != 1f) > 0;

                if (projectileModificationExists || weaponModificationExists)
                {
                    // Get then apply modification to damage
                    if (projectileModificationExists)
                    {
                        var projectileModification = Controller.Projectiles.FirstOrDefault(p => p.netID == sourceProjectileType);
                        safeDamage = Convert.ToInt16(safeDamage * projectileModification.damageRatio);
                    }

                    if (weaponModificationExists)
                    {
                        var weaponModification = Controller.Weapons.FirstOrDefault(p => p.netID == weapon.netID);
                        safeDamage = Convert.ToInt16(safeDamage * weaponModification.damageRatio);
                    }
                    
                    realDamage = (int)Main.CalculatePlayerDamage((int)safeDamage, Main.player[playerId].statDefense);
                    realDamage = (int)Math.Round(realDamage * (1 - Main.player[playerId].endurance));

                    // Send the combat text
                    msgColor = new Color(162, 0, 255);
                    NetMessage.SendData((int)PacketTypes.CreateCombatText, index, -1, NetworkText.FromLiteral($"{realDamage}"), (int)msgColor.PackedValue, Main.player[playerId].position.X, Main.player[playerId].position.Y - 32);

                    ApplyPlayerDamage(args.Player.TshockPlayer, Controller.Players[playerId], weapon, dir, (int)safeDamage, realDamage);
                    NetMessage.SendData((int)PacketTypes.PlayerHp, -1, playerId, "", playerId);
                    return true;
                }
            }

            // Send the damage number to show the player the real damage
            msgColor = new Color(162, 0, 255);
            realDamage = (int)Main.CalculatePlayerDamage((int)safeDamage, Main.player[playerId].statDefense);
            realDamage = (int)Math.Round(realDamage * (1 - Main.player[playerId].endurance));

            // Send Damage and Damage Text
            NetMessage.SendData((int)PacketTypes.CreateCombatText, index, -1, NetworkText.FromLiteral($"{realDamage}"), (int)msgColor.PackedValue, Main.player[playerId].position.X, Main.player[playerId].position.Y - 32);
            ApplyPlayerDamage(args.Player.TshockPlayer, Controller.Players[playerId], weapon, dir, (int)safeDamage, realDamage);
            Killers[playerId] = new PlayerKiller(args.Player.TshockPlayer, weapon);
            NetMessage.SendData((int)PacketTypes.PlayerHp, -1, playerId, "", playerId);
            return true;
        }

        /// <summary>
        /// Ensures that a player is forbidden from teleporting while in pvp
        /// </summary>
        /// <param name="args">The GetDataHandlerArgs object containing the player who sent the packet and the
        /// data in it</param>
        /// <returns>Whether or not the packet was handled (and should therefore not be processed
        /// by anything else</returns>
        private bool HandlePlayerTeleport(GetDataHandlerArgs args)
        {
            if (!Controller.Config.BanTeleportItems)
            {
                return false;
            }

            if (args.Player.TPlayer.hostile)
            {
                args.Player.TshockPlayer.SendData(PacketTypes.Teleport, "", 0, args.Player.Index, args.Player.TshockPlayer.LastNetPosition.X, args.Player.TshockPlayer.LastNetPosition.Y);
                args.Player.TshockPlayer.SetBuff(149, 60);
            }

            return args.Player.TPlayer.hostile;
        }

        /// <summary>
        /// Ensures that a player is forbidden from teleporting while in pvp
        /// </summary>
        /// <param name="args">The GetDataHandlerArgs object containing the player who sent the packet and the
        /// data in it</param>
        /// <returns>Whether or not the packet was handled (and should therefore not be processed
        /// by anything else</returns>
        private bool HandlePlayerTeleportPotion(GetDataHandlerArgs args)
        {
            return HandlePlayerTeleport(args);
        }

        /// <summary>
        /// Ensures that a player is forbidden from teleporting while in pvp
        /// </summary>
        /// <param name="args">The GetDataHandlerArgs object containing the player who sent the packet and the
        /// data in it</param>
        /// <returns>Whether or not the packet was handled (and should therefore not be processed
        /// by anything else</returns>
        private bool HandlePlayerSpawn(GetDataHandlerArgs args)
        {
            bool handled = false;
            if (Controller.Config.BanTeleportItems && args.Player.TPlayer.hostile && !args.Player.IsDead)
            {
                args.Player.TshockPlayer.SendData(PacketTypes.Teleport, "", 0, args.Player.Index, args.Player.TshockPlayer.LastNetPosition.X, args.Player.TshockPlayer.LastNetPosition.Y);
                args.Player.TshockPlayer.SetBuff(149, 60);
                handled = true;
            } else if (args.Player.IsDead)
            {
                args.Player.TPlayer.statLifeMax = 500;
                args.Player.TPlayer.statLifeMax2 = 600;
                args.Player.TPlayer.statLife = args.Player.TPlayer.statLifeMax2;
                args.Player.ForceActiveHealth(args.Player.TPlayer.statLife);
            }

            args.Player.IsDead = false;
            args.Player.Spectating = false;
            args.Player.LastHeal = DateTime.Now.AddSeconds(-Controller.Config.PotionHealCooldown);
            return handled;
        }
        
        /// <summary>
        /// Prevents prefixes on armor items
        /// </summary>
        /// <param name="args">The GetDataHandlerArgs object containing the player who sent the packet and the
        /// data in it</param>
        /// <returns>Whether or not the packet was handled (and should therefore not be processed
        /// by anything else)</returns>
        private bool HandleInventoryUpdate(GetDataHandlerArgs args)
        {
            args.Data.ReadByte();
            int slotId = args.Data.ReadByte();
            args.Data.ReadInt16();
            int prefix = args.Data.ReadByte();
            int netId = args.Data.ReadInt16();

            // Is armor
            if (Controller.Config.BanPrefixedArmor && prefix > 0 && slotId >= 59 && slotId <= 61)
            {
                args.Player.ForceItem(slotId, 0, netId, 1);
            }

            return false;
        }

        /// <summary>
        /// Handles when a client tries to heal
        /// </summary>
        /// <param name="args">The GetDataHandlerArgs object containing the player who sent the packet and the
        /// data in it</param>
        /// <returns></returns>
        private bool HandleEffectHeal(GetDataHandlerArgs args)
        {
            bool handled = false;

            if (args.Player.TPlayer.hostile)
            {
                handled = true;

                if ((DateTime.Now - args.Player.LastHeal).TotalSeconds > Controller.Config.PotionHealCooldown)
                {
                    args.Player.LastHeal = DateTime.Now;
                    ApplyPlayerHeal(args.Player, Controller.Config.PotionHealAmt);
                }
            }

            return handled;
        }

        /// <summary>
        /// Strips any repeated buffs, then broadcast and stores them as a server normally would
        /// </summary>
        /// <param name="args">The GetDataHandlerArgs object containing the player who sent the packet and the
        /// data in it</param>
        private bool HandlePlayerBuffs(GetDataHandlerArgs args)
        {
            args.Data.ReadByte();

            var buffs = new Dictionary<int, bool>();
            int currentBuffType;
            for (int buffTypeIndex = 0; buffTypeIndex < 22; buffTypeIndex++) {
                args.Player.TPlayer.buffType[buffTypeIndex] = 0;

                currentBuffType = args.Data.ReadByte();
                if (!buffs.ContainsKey(currentBuffType))
                {
                    buffs.Add(currentBuffType, true);
                    args.Player.TPlayer.buffType[buffTypeIndex] = currentBuffType;
                    args.Player.TPlayer.buffTime[buffTypeIndex] = args.Player.TPlayer.buffType[buffTypeIndex] <= 0 ? 0 : 60;
                }
            }

            NetMessage.SendData(50, -1, args.Player.TPlayer.whoAmI, null, args.Player.TPlayer.whoAmI, 0.0f, 0.0f, 0.0f, 0, 0, 0);

            return true;
        }

        /// <summary>
        /// Applies a player heal for the given amount, not going over the players max hp
        /// </summary>
        /// <param name="player">The player who is healing</param>
        /// <param name="healAmt">The amount they are healing for</param>
        private void ApplyPlayerHeal(Player player, int healAmt)
        {
            player.TPlayer.statLife += healAmt;
            if (player.TPlayer.statLife > player.TPlayer.statLifeMax2)
            {
                player.TPlayer.statLife = player.TPlayer.statLifeMax2;
            }

            player.ForceActiveHealth(player.TPlayer.statLife);
        }

        /// <summary>
        /// Applies an amount of damage to player, updating their client and the server version of their health
        /// </summary>
        /// <param name="killer"></param>
        /// <param name="victim"></param>
        /// <param name="killersWeapon"></param>
        /// <param name="dir"></param>
        /// <param name="damage"></param>
        /// <param name="realDamage"></param>
        private void ApplyPlayerDamage(TSPlayer killer, Player victim, Item killersWeapon, int dir, int damage, int realDamage)
        {
            // Send the damage using the special method to avoid invincibility frames issue
            SendPlayerDamage(victim.TshockPlayer, dir, damage);
            Controller.RaisePlayerDamageEvent(this, new PlayerDamageEventArgs(killer, victim.TshockPlayer, killersWeapon, realDamage));
            victim.TPlayer.statLife -= realDamage;

            // Hurt the player to prevent instant regen activating
            var savedHealth = victim.TPlayer.statLife;
            victim.TPlayer.Hurt(new PlayerDeathReason(), damage, 0, true, false, false, 3);
            victim.TPlayer.statLife = savedHealth;

            if (victim.TPlayer.statLife <= 0)
            {
                victim.TshockPlayer.Dead = true;
                victim.IsDead = true;
                SendPlayerDeath(victim.TshockPlayer);

                if (victim.TPlayer.hostile && Killers[victim.Index] != null)
                {
                    PlayerKillEventArgs killArgs = new PlayerKillEventArgs(Killers[victim.Index].Player, victim.TshockPlayer, Killers[victim.Index].Weapon);
                    Controller.RaisePlayerKillEvent(this, killArgs);
                    Killers[victim.Index] = null;
                }
                return;
            }

            victim.ForceActiveHealth(victim.TPlayer.statLife);
        }

        /// <summary>
        /// Sends a raw packet built from base values to both fix the invincibility frames
        /// and provide a way to modify incoming damage.
        /// </summary>
        /// <param name="player">The TSPlayer object of the player to get hurt</param>
        /// <param name="hitDirection">The hit direction (left or right, -1 or 1)</param>
        /// <param name="damage">The amount of damage to deal to the player</param>
        private void SendPlayerDamage(TSPlayer player, int hitDirection, int damage)
        {
            // This flag permutation gives low invinc frames for proper client
            // sync
            BitsByte flags = new BitsByte();
            flags[0] = true; // PVP
            flags[1] = false; // Crit
            flags[2] = false; // Cooldown -1
            flags[3] = false; // Cooldown +1
            byte[] playerDamage = new PacketFactory()
                .SetType((short)PacketTypes.PlayerHurtV2)
                .PackByte((byte)player.Index)
                .PackByte(0)
                .PackInt16((short)damage)
                .PackByte((byte)hitDirection)
                .PackByte(flags)
                .PackByte(3)
                .GetByteData();

            foreach (var plr in TShock.Players)
            {
                if (plr != null)
                    plr.SendRawData(playerDamage);
            }
        }

        private void SendPlayerDeath(TSPlayer player)
        {
            byte[] playerDeath = new PacketFactory()
                .SetType((short)PacketTypes.PlayerDeathV2)
                .PackByte((byte)player.Index)
                .PackByte(0)
                .PackInt16(0)
                .PackByte(0)
                .PackByte(1)
                .GetByteData();

            foreach (var plr in TShock.Players)
            {
                if (plr != null)
                    plr.SendRawData(playerDeath);
            }
        }
    }
}