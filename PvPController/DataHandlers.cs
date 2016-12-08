using System;
using System.IO;
using System.IO.Streams;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using TShockAPI;
using System.Diagnostics;
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

    /* Contains the handlers for certain packets received by the server */
    internal static class GetDataHandlers
    {
        private static Dictionary<PacketTypes, GetDataHandlerDelegate> _getDataHandlerDelegates;

        /* Adds the handler functions as handlers for the given PacketType */
        public static void InitGetDataHandler()
        {
            _getDataHandlerDelegates = new Dictionary<PacketTypes, GetDataHandlerDelegate>
            {
                {PacketTypes.ProjectileNew, HandleProjectile},
                {PacketTypes.PlayerHurtV2, HandleDamage},
                {PacketTypes.PlayerDamage, HandleOldDamage },
                {PacketTypes.PlayerUpdate, HandlePlayerUpdate},
            };
        }

        /* Checks if there is a handler for the given packet type and will return
         * whether or not the packet was handled
         * 
         * @param type
         *          The PacketType
         *          
         * @param player
         *          The player that sent the packet
         *          
         * @param data
         *          The packet data
         * 
         * @return
         *          Whether or not the packet was handled (and should therefore not be processed
         *          by anything else)
         */
        public static bool HandlerGetData(PacketTypes type, TSPlayer player, MemoryStream data)
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
        
        public static DateTime[] LastMessage = new DateTime[256];

        /* Handles the case when the player shoots a banned projectile and
         * starts a stopwatch from the moment they use it, or reset
         * the existing stopwatch. It also updates the array of ProjectileWeapon
         * so that the projectile can be traced back to its weapon.
         * 
         * @param args
         *          The GetDataHandlerArgs containing the player that sent the packet and the data of the packet
         * 
         * @return
         *          Whether or not the packet was handled (and should therefore not be processed
         *          by anything else)
         */
        private static bool HandleProjectile(GetDataHandlerArgs args)
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
                if (PvPController.projectiles.Count(p => p.netID == type && p.banned) > 0)
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

                    if (PvPController.weapons.Count(p => p.netID == weaponUsed.netID && p.buffs.Count() > 0) > 0)
                    {
                        var proj = new Terraria.Projectile();
                        proj.SetDefaults(type);

                        if (proj.ranged && dmg > 0)
                        {
                            var weapon = PvPController.weapons.FirstOrDefault(p => p.netID == args.Player.SelectedItem.netID);
                            var weaponBuffs = weapon.buffs;
                            foreach (var weaponBuff in weaponBuffs)
                            {
                                args.Player.SetBuff(weaponBuff.netID, Convert.ToInt32((weaponBuff.milliseconds / 1000f) * 60), true);
                            }
                        }
                    }

                    var modification = PvPController.projectiles.FirstOrDefault(p => p.netID == type);
                    StorageTypes.Weapon weaponModification = null;
                    if (dmg > 0) {
                       weaponModification = PvPController.weapons.FirstOrDefault(p => p.netID == args.Player.SelectedItem.netID);
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

        /* Handles the case when the player uses a banned item and
         * warns the player that the weapon does no damage.
         * 
         * @param args
         *          The GetDataHandlerArgs containing the player that sent the packet and the data of the packet
         * 
         * @return
         *          Whether or not the packet was handled (and should therefore not be processed
         *          by anything else)
         */
        private static bool HandlePlayerUpdate(GetDataHandlerArgs args)
        {
            byte plr = args.Data.ReadInt8();
            BitsByte control = args.Data.ReadInt8();
            BitsByte pulley = args.Data.ReadInt8();
            byte item = args.Data.ReadInt8();
            var pos = new Vector2(args.Data.ReadSingle(), args.Data.ReadSingle());
            var vel = Vector2.Zero;
            
            if (control[5] && PvPController.weapons.Count(p => p.netID == args.Player.SelectedItem.netID && p.banned) > 0 && args.Player.TPlayer.hostile)
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
        
        /* Determines whether to block damage if they have recently used a banned item
         * or modifies damage if the projectile is on the modifications list.
         * 
         * @param args
         *          The GetDataHandlerArgs object containing the player who sent the packet
         *          and the data in it.
         * 
         * @return
         *          Whether or not the packet was handled (and should therefore not be processed
         *          by anything else)
         */
        private static bool HandleDamage(GetDataHandlerArgs args)
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
            if (PvPController.weapons.Count(p => p.netID == weapon.netID && p.banned) > 0 || PvPController.projectiles.Count(p => p.netID == sourceProjectileType && p.banned) > 0)
            {
                args.Player.SendData(PacketTypes.PlayerHp, "", playerId);
                return true;
            }
            else
            {
                // Get the weapon and whether a modification exists
                var weaponModificationExists = PvPController.weapons.Count(p => p.netID == weapon.netID && p.damageRatio != 1f) > 0;
                var projectileModificationExists = PvPController.projectiles.Count(p => p.netID == sourceProjectileType && p.damageRatio != 1f) > 0;

                if (projectileModificationExists || weaponModificationExists)
                {
                    // Get then apply modification to damage
                    if (projectileModificationExists)
                    {
                        var projectileModification = PvPController.projectiles.FirstOrDefault(p => p.netID == sourceProjectileType);
                        safeDamage = Convert.ToInt16(safeDamage * projectileModification.damageRatio);
                    }

                    if (weaponModificationExists)
                    {
                        var weaponModification = PvPController.weapons.FirstOrDefault(p => p.netID == weapon.netID);
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
                    return true;
                }
            }

            // Send the damage number to show the player the real damage
            msgColor = new Color(162, 0, 255);
            realDamage = (int)Main.CalculatePlayerDamage((int)safeDamage, Main.player[playerId].statDefense);
            realDamage = (int)Math.Round(realDamage * (1 - Main.player[playerId].endurance));
            NetMessage.SendData((int)PacketTypes.CreateCombatText, index, -1, $"{realDamage}", (int)msgColor.PackedValue, Main.player[playerId].position.X, Main.player[playerId].position.Y - 32);
            SendPlayerDamage(TShock.Players[playerId], dir, (int)safeDamage);
            return true;
        }
        
        private static bool HandleOldDamage(GetDataHandlerArgs args)
        {
            return true;
        }

        /* Sends a raw packet built from base values to both fix the invincibility frames
         * and provide a way to modify incoming damage.
         * 
         * @param player
         *          The TSPlayer object of the player to get hurt
         *          
         * @param hitDirection
         *          The hit direction (left or right, -1, 1)
         *          
         * @param damage
         *          The amount of damage to deal to the player
         */
        private static void SendPlayerDamage(TSPlayer player, int hitDirection, int damage)
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