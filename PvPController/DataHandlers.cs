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
        private bool[] isDead = new bool[256];


        /* Adds the handler functions as handlers for the given PacketType */
        public GetDataHandlers(PvPController controller)
        {
            this.Controller = controller;

            _getDataHandlerDelegates = new Dictionary<PacketTypes, GetDataHandlerDelegate>
            {
                {PacketTypes.ProjectileNew, HandleProjectile},
                {PacketTypes.PlayerHurtV2, HandleDamage},
                {PacketTypes.PlayerUpdate, HandlePlayerUpdate},
                { PacketTypes.Teleport, HandlePlayerTeleport },
                { PacketTypes.TeleportationPotion, HandlePlayerTeleportPotion },
                { PacketTypes.PlayerSpawn, HandlePlayerSpawn },
                { PacketTypes.PlayerSlot, HandleInventoryUpdate }
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

            if (args.Player.TPlayer.hostile)
            {
                if (Controller.Projectiles.Count(p => p.netID == type && p.banned) > 0)
                {
                    args.Player.RemoveProjectileAndTellIsIneffective(Controller.Config.HideDisallowedProjectiles, ident);
                }
                else
                {
                    Item weaponUsed = args.Player.TshockPlayer.SelectedItem;
                    if (Main.projectile[ident].active == false || Main.projectile[ident].owner == owner)
                    {
                        weaponUsed = ProjectileMapper.DetermineWeaponUsed(type, args.Player);
                    }

                    if (Controller.Weapons.Count(p => p.netID == weaponUsed.netID && p.buffs.Count() > 0) > 0)
                    {
                        var proj = new Terraria.Projectile();
                        proj.SetDefaults(type);

                        if (proj.ranged && dmg > 0)
                        {
                            var weapon = Controller.Weapons.FirstOrDefault(p => p.netID == args.Player.TshockPlayer.SelectedItem.netID);
                            var weaponBuffs = weapon.buffs;
                            foreach (var weaponBuff in weaponBuffs)
                            {
                                args.Player.TshockPlayer.SetBuff(weaponBuff.netID, Convert.ToInt32((weaponBuff.milliseconds / 1000f) * 60), true);
                            }
                        }
                    }

                    var modification = Controller.Projectiles.FirstOrDefault(p => p.netID == type);
                    StorageTypes.Weapon weaponModification = null;
                    if (dmg > 0) {
                       weaponModification = Controller.Weapons.FirstOrDefault(p => p.netID == args.Player.TshockPlayer.SelectedItem.netID);
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
            
            if (control[5] && Controller.Weapons.Count(p => p.netID == args.Player.TshockPlayer.SelectedItem.netID && p.banned) > 0 && args.Player.TPlayer.hostile)
            {
                args.Player.TellWeaponIsIneffective();
            }

            return false;
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
                if (args.Player == null) return false;

                if (args.Player.TPlayer.hostile && Killers[args.Player.Index] != null)
                {
                    PlayerKillEventArgs killArgs = new PlayerKillEventArgs(Killers[args.Player.Index].Player, args.Player.TshockPlayer, Killers[args.Player.Index].Weapon);
                    Controller.RaisePlayerKillEvent(this, killArgs);
                    Killers[args.Player.Index] = null;
                }
            }
            catch (Exception e)
            {
                TShock.Log.ConsoleError(e.ToString());
            }

            isDead[args.Player.Index] = true;

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
                    Main.player[playerId].Hurt(new PlayerDeathReason(), (int)safeDamage, 1, true, false, false, 3);

                    /* Send out the HP value to the client who now has the wrong value
                        * (due to health being update before the packet is sent) can use the right one */
                    args.Player.TshockPlayer.SendData(PacketTypes.PlayerHp, "", playerId);


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

            // Send Damage and Damage Text
            NetMessage.SendData((int)PacketTypes.CreateCombatText, index, -1, $"{realDamage}", (int)msgColor.PackedValue, Main.player[playerId].position.X, Main.player[playerId].position.Y - 32);
            SendPlayerDamage(TShock.Players[playerId], dir, (int)safeDamage);


            Killers[playerId] = new PlayerKiller(args.Player.TshockPlayer, weapon);
            Controller.RaisePlayerDamageEvent(this, new PlayerDamageEventArgs(args.Player.TshockPlayer, TShock.Players[playerId], weapon, realDamage));
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
            if (!Controller.Config.BanTeleportItems)
            {
                isDead[args.Player.Index] = false;
                return false;
            }

            if (args.Player.TPlayer.hostile && !isDead[args.Player.Index])
            {
                args.Player.TshockPlayer.SendData(PacketTypes.Teleport, "", 0, args.Player.Index, args.Player.TshockPlayer.LastNetPosition.X, args.Player.TshockPlayer.LastNetPosition.Y);
                args.Player.TshockPlayer.SetBuff(149, 60);
            }

            isDead[args.Player.Index] = false;
            return args.Player.TPlayer.hostile;
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
                ForceItem(args.Player.TshockPlayer, slotId, 0, netId, 1);
            }

            return false;
        }

        /// <summary>
        /// Forces a slot to a specific item regardless of SSC
        /// </summary>
        /// <param name="player">The player that is being updated</param>
        /// <param name="slotId">The slot id of the slot to update</param>
        /// <param name="prefix">The prefix to set on the item</param>
        /// <param name="netId">The netId to set on the item</param>
        public void ForceItem(TSPlayer player, int slotId, int prefix, int netId, int stack)
        {
            ForceClientSSC(true, player);
            ForceServerItem(player.TPlayer, slotId, prefix, netId, stack);
            ForceClientSSC(false, player);
        }

        /// <summary>
        /// Forces a slot to a specific item in the server storage of the players inventory and broadcasts the update
        /// </summary>
        /// <param name="player"></param>
        /// <param name="slotId"></param>
        /// <param name="prefix"></param>
        /// <param name="netId"></param>
        /// <param name="stack"></param>
        private void ForceServerItem(Terraria.Player player, int slotId, int prefix, int netId, int stack)
        {
            if (slotId < NetItem.InventorySlots)
            {
                //58
                player.inventory[slotId].netDefaults(netId);

                if (player.inventory[slotId].netID != 0)
                {
                    player.inventory[slotId].stack = stack;
                    player.inventory[slotId].prefix = (byte)prefix;
                    NetMessage.SendData(5, -1, -1, player.inventory[slotId].name, player.whoAmI, slotId, player.inventory[slotId].prefix, player.inventory[slotId].stack);
                }
            }
            else if (slotId < NetItem.InventorySlots + NetItem.ArmorSlots)
            {
                //59-78
                var index = slotId - NetItem.InventorySlots;
                player.armor[index].netDefaults(netId);

                if (player.armor[index].netID != 0)
                {
                    player.armor[index].stack = stack;
                    player.armor[index].prefix = (byte)prefix;
                    NetMessage.SendData(5, -1, -1, player.armor[index].name, player.whoAmI, slotId, player.armor[index].prefix, player.armor[index].stack);
                }
            }
            else if (slotId < NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots)
            {
                //79-88
                var index = slotId - (NetItem.InventorySlots + NetItem.ArmorSlots);
                player.dye[index].netDefaults(netId);

                if (player.dye[index].netID != 0)
                {
                    player.dye[index].stack = stack;
                    player.dye[index].prefix = (byte)prefix;
                    NetMessage.SendData(5, -1, -1, player.dye[index].name, player.whoAmI, slotId, player.dye[index].prefix, player.dye[index].stack);
                }
            }
            else if (slotId <
                NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.MiscEquipSlots)
            {
                //89-93
                var index = slotId - (NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots);
                player.miscEquips[index].netDefaults(netId);

                if (player.miscEquips[index].netID != 0)
                {
                    player.miscEquips[index].stack = stack;
                    player.miscEquips[index].prefix = (byte)prefix;
                    NetMessage.SendData(5, -1, -1, player.miscEquips[index].name, player.whoAmI, slotId, player.miscEquips[index].prefix, player.miscEquips[index].stack);
                }
            }
            else if (slotId <
                NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.MiscEquipSlots
                + NetItem.MiscDyeSlots)
            {
                //93-98
                var index = slotId - (NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots
                    + NetItem.MiscEquipSlots);
                player.miscDyes[index].netDefaults(netId);

                if (player.miscDyes[index].netID != 0)
                {
                    player.miscDyes[index].stack = stack;
                    player.miscDyes[index].prefix = (byte)prefix;
                    NetMessage.SendData(5, -1, -1, player.miscDyes[index].name, player.whoAmI, slotId, player.miscDyes[index].prefix, player.miscDyes[index].stack);
                }
            }
        }

        /// <summary>
        /// Forces a clients SSC to a specific value
        /// </summary>
        /// <param name="on">Whether SSC is to be set to on or not</param>
        private void ForceClientSSC(bool on, TSPlayer player)
        {

            Main.ServerSideCharacter = on;
            NetMessage.SendData((int)PacketTypes.WorldInfo, player.Index, -1, "");
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
    }
}