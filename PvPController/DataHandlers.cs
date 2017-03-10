using System;
using System.IO;
using System.IO.Streams;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using TShockAPI;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;

namespace PvPController
{
    internal delegate bool GetDataHandlerDelegate(GetDataHandlerArgs args);

    internal class GetDataHandlerArgs : EventArgs
    {
        public TSPlayer Player { get; private set; }
        public MemoryStream Data { get; private set; }

        public GetDataHandlerArgs(TSPlayer player, MemoryStream data)
        {
            Player = player;
            Data = data;
        }
    }

    /// <summary>
    /// Contains the handlers for certain packets received by the server
    /// </summary>
    internal class GetDataHandlers
    {
        public DateTime[] LastMessage = new DateTime[256];
        private PlayerKiller[] Killers = new PlayerKiller[255];
        private Dictionary<PacketTypes, GetDataHandlerDelegate> _getDataHandlerDelegates;

        /// <summary>
        /// Adds the handler functions as handlers for the given PacketType
        /// </summary>
        public GetDataHandlers()
        {
            _getDataHandlerDelegates = new Dictionary<PacketTypes, GetDataHandlerDelegate>
            {
                { PacketTypes.ProjectileNew, HandleProjectile },
                { PacketTypes.PlayerHurtV2, HandleDamage },
                { PacketTypes.PlayerDeathV2, HandleDeath },
                { PacketTypes.PlayerUpdate, HandlePlayerUpdate},
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
        public bool HandlerGetData(PacketTypes type, TSPlayer player, MemoryStream data)
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
            float[] ai = new float[Terraria.Projectile.maxAI];

            if (args.Player.TPlayer.hostile)
            {
                if (PvPController.Projectiles.Count(p => p.netID == type && p.banned) > 0)
                {
                    var proj = Main.projectile[ident];
                    proj.active = false;
                    proj.type = 0;
                    if (PvPController.Config.HideDisallowedProjectiles)
                    {
                        TSPlayer.All.SendData(PacketTypes.ProjectileDestroy, "", ident);
                    }
                    proj.owner = 255;
                    proj.active = false;
                    proj.type = 0;
                    TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", ident);

                    if ((DateTime.Now - LastMessage[args.Player.Index]).TotalSeconds > 2)
                    {
                        args.Player.SendMessage("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++", Color.Red);
                        args.Player.SendMessage("That projectile does not work in PVP. Using it will cause you to do no damage!", Color.Red);
                        args.Player.SendMessage("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++", Color.Red);
                        LastMessage[args.Player.Index] = DateTime.Now;
                    }
                    return true;
                }
                else
                {
                    Item weaponUsed = args.Player.SelectedItem;

                    // Check that they either own the projectile (existing), or it is inactive (and therefore this is a new one)
                    if (Main.projectile[ident].active == false || Main.projectile[ident].owner == owner)
                    {
                        weaponUsed = ProjectileMapper.DetermineWeaponUsed(type, args.Player);
                    }

                    if (PvPController.Weapons.Count(p => p.netID == weaponUsed.netID && p.buffs.Count() > 0) > 0)
                    {
                        var proj = new Terraria.Projectile();
                        proj.SetDefaults(type);

                        if (proj.ranged && dmg > 0)
                        {
                            var weapon = PvPController.Weapons.FirstOrDefault(p => p.netID == args.Player.SelectedItem.netID);
                            var weaponBuffs = weapon.buffs;
                            foreach (var weaponBuff in weaponBuffs)
                            {
                                args.Player.SetBuff(weaponBuff.netID, Convert.ToInt32((weaponBuff.milliseconds / 1000f) * 60), true);
                            }
                        }
                    }

                    var modification = PvPController.Projectiles.FirstOrDefault(p => p.netID == type);
                    StorageTypes.Weapon weaponModification = null;
                    if (dmg > 0)
                    {
                        weaponModification = PvPController.Weapons.FirstOrDefault(p => p.netID == args.Player.SelectedItem.netID);
                    }
                    if ((modification != null && modification.velocityRatio != 1f) || (weaponModification != null && weaponModification.velocityRatio != 1f))
                    {
                        var proj = Main.projectile[ident];
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
                        NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, "", ident);
                        return true;
                    }
                }
            }

            return false;
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

            if (control[5] && PvPController.Weapons.Count(p => p.netID == args.Player.SelectedItem.netID && p.banned) > 0 && args.Player.TPlayer.hostile)
            {
                if ((DateTime.Now - LastMessage[args.Player.Index]).TotalSeconds > 2)
                {
                    args.Player.SendMessage("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++", Color.Red);
                    args.Player.SendMessage("That weapon does not work in PVP. Using it will cause you to do no damage!", Color.Red);
                    args.Player.SendMessage("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++", Color.Red);
                    LastMessage[args.Player.Index] = DateTime.Now;
                }
            }

            return false;
        }

        /// <summary>
        /// Raises an event about who and with what weapon a player was killed
        /// </summary>
        /// <param name="args">The GetDataHandlersArgs object containing the player who sent the
        /// packet and the data in it</param>
        /// <returns>Whether or not the packet was handled (and should therefore not be processed
        /// by anything else)</returns>
        private bool HandleDeath(GetDataHandlerArgs args)
        {
            try
            {
                if (args.Player == null) return false;

                if (args.Player.TPlayer.hostile && Killers[args.Player.Index] != null)
                {
                    PlayerKillEventArgs killArgs = new PlayerKillEventArgs(Killers[args.Player.Index].Player, args.Player, Killers[args.Player.Index].Weapon);
                    PvPController.RaisePlayerKillEvent(this, killArgs);
                    Killers[args.Player.Index] = null;
                }
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
        /// <param name="args">The GetDataHandlerArgs object containging the player who sent the packet and the
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

            if (TShock.Players[playerId] == null || sourceItemType == -1)
                return false;

            // The sourceItemType is only reliable if no projectile is involved
            // as sourceItemType is simply the active slot item
            Item weapon = new Item();
            if (sourceProjectileType == -1)
            {
                weapon.SetDefaults(sourceItemType);
                weapon.prefix = (byte)sourceItemPrefix;
                weapon.owner = args.Player.Index;
            }
            else if (PvPController.ProjectileWeapon[args.Player.Index, sourceProjectileType] != null)
            {
                Item bestWeaponGuess = PvPController.ProjectileWeapon[args.Player.Index, sourceProjectileType];
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
            if (PvPController.Weapons.Count(p => p.netID == weapon.netID && p.banned) > 0 || PvPController.Projectiles.Count(p => p.netID == sourceProjectileType && p.banned) > 0)
            {
                args.Player.SendData(PacketTypes.PlayerHp, "", playerId);
                return true;
            }
            else
            {
                // Get the weapon and whether a modification exists
                var weaponModificationExists = PvPController.Weapons.Count(p => p.netID == weapon.netID && p.damageRatio != 1f) > 0;
                var projectileModificationExists = PvPController.Projectiles.Count(p => p.netID == sourceProjectileType && p.damageRatio != 1f) > 0;

                if (projectileModificationExists || weaponModificationExists)
                {
                    // Get then apply modification to damage
                    if (projectileModificationExists)
                    {
                        var projectileModification = PvPController.Projectiles.FirstOrDefault(p => p.netID == sourceProjectileType);
                        safeDamage = Convert.ToInt16(safeDamage * projectileModification.damageRatio);
                    }

                    if (weaponModificationExists)
                    {
                        var weaponModification = PvPController.Weapons.FirstOrDefault(p => p.netID == weapon.netID);
                        safeDamage = Convert.ToInt16(safeDamage * weaponModification.damageRatio);
                    }

                    realDamage = (int)Main.CalculatePlayerDamage((int)safeDamage, Main.player[playerId].statDefense);
                    realDamage = (int)Math.Round(realDamage * (1 - Main.player[playerId].endurance));
                    Main.player[playerId].Hurt(new PlayerDeathReason(), (int)safeDamage, 1, true, false, false, 3);

                    /* Send out the HP value to the client who now has the wrong value
                        * (due to health being update before the packet is sent) can use the right one */
                    args.Player.SendData(PacketTypes.PlayerHp, "", playerId);


                    // Send the combat text
                    msgColor = new Color(162, 0, 255);
                    NetMessage.SendData((int)PacketTypes.CreateCombatText, index, -1, $"{realDamage}", (int)msgColor.PackedValue, Main.player[playerId].position.X, Main.player[playerId].position.Y - 32);

                    // Send the damage using the special method to avoid invincibility frames issue
                    SendPlayerDamage(TShock.Players[playerId], dir, (int)safeDamage);

                    Killers[playerId] = new PlayerKiller(args.Player, weapon);
                    PvPController.RaisePlayerDamageEvent(this, new PlayerDamageEventArgs(args.Player, TShock.Players[playerId], weapon, realDamage));
                    return true;
                }
            }

            // Send the damage number to show the player the real damage
            msgColor = new Color(162, 0, 255);
            realDamage = (int)Main.CalculatePlayerDamage((int)safeDamage, Main.player[playerId].statDefense);
            realDamage = (int)Math.Round(realDamage * (1 - Main.player[playerId].endurance));

            // Send Damage and Damage Text
            NetMessage.SendData((int)PacketTypes.CreateCombatText, index, -1, $"{realDamage}", (int)msgColor.PackedValue, Main.player[playerId].position.X, Main.player[playerId].position.Y - 32);
            SendPlayerDamage(TShock.Players[playerId], dir, (int)safeDamage);

            Killers[playerId] = new PlayerKiller(args.Player, weapon);
            PvPController.RaisePlayerDamageEvent(this, new PlayerDamageEventArgs(args.Player, TShock.Players[playerId], weapon, realDamage));
            return true;
        }

        /// <summary>
        /// Sends a raw packet built from base values to both fix the invincibility frames
        /// and provide a way to modify incoming damage.
        /// </summary>
        /// <param name="player">The TSPLayer object of the player to get hurt</param>
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
    }
}