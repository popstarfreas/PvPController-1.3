using TShockAPI;
using Terraria;
using Terraria.Localization;

namespace PvPController
{
    internal class DataSender
    {


        /// <summary>
        /// Forces a players active health to a given value
        /// </summary>
        /// <param name="player">The player that is being updated</param>
        /// <param name="health">The new health value</param>
        internal void SendClientHealth(Player player, int health)
        {
            ForceClientSSC(player, true);
            player.TPlayer.statLife = health;
            NetMessage.SendData((int)PacketTypes.PlayerHp, -1, -1, NetworkText.Empty, player.Index);
            ForceClientSSC(player, false);
        }

        /// <summary>
        /// Forces a slot to a specific item regardless of SSC
        /// </summary>
        /// <param name="player">The player that is being updated</param>
        /// <param name="slotId">The slot id of the slot to update</param>
        /// <param name="prefix">The prefix to set on the item</param>
        /// <param name="netId">The netId to set on the item</param>
        internal void SendSlotUpdate(Player player, int slotId, Item newItem)
        {
            ForceClientSSC(player, true);
            ForceServerItem(player, slotId, newItem);
            ForceClientSSC(player, false);
        }

        /// <summary>
        /// Forces a slot to a specific item in the server storage of the players inventory and broadcasts the update
        /// </summary>
        /// <param name="player"></param>
        /// <param name="slotId"></param>
        /// <param name="prefix"></param>
        /// <param name="netId"></param>
        /// <param name="stack"></param>
        internal void ForceServerItem(Player player, int slotId, Item newItem)
        {
            if (slotId < NetItem.InventorySlots)
            {
                //58
                player.TPlayer.inventory[slotId].netDefaults(newItem.netID);

                if (player.TPlayer.inventory[slotId].netID != 0)
                {
                    player.TPlayer.inventory[slotId] = newItem;
                    NetMessage.SendData(5, -1, -1, NetworkText.Empty, player.TPlayer.whoAmI, slotId, player.TPlayer.inventory[slotId].prefix, player.TPlayer.inventory[slotId].stack);
                }
            }
            else if (slotId < NetItem.InventorySlots + NetItem.ArmorSlots)
            {
                //59-78
                var index = slotId - NetItem.InventorySlots;
                player.TPlayer.armor[index].netDefaults(newItem.netID);

                if (player.TPlayer.armor[index].netID != 0)
                {
                    player.TPlayer.armor[index] = newItem;
                    NetMessage.SendData(5, -1, -1, NetworkText.Empty, player.TPlayer.whoAmI, slotId, player.TPlayer.armor[index].prefix, player.TPlayer.armor[index].stack);
                }
            }
            else if (slotId < NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots)
            {
                //79-88
                var index = slotId - (NetItem.InventorySlots + NetItem.ArmorSlots);
                player.TPlayer.dye[index].netDefaults(newItem.netID);

                if (player.TPlayer.dye[index].netID != 0)
                {
                    player.TPlayer.dye[index] = newItem;
                    NetMessage.SendData(5, -1, -1, NetworkText.Empty, player.TPlayer.whoAmI, slotId, player.TPlayer.dye[index].prefix, player.TPlayer.dye[index].stack);
                }
            }
            else if (slotId <
                NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.MiscEquipSlots)
            {
                //89-93
                var index = slotId - (NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots);
                player.TPlayer.miscEquips[index].netDefaults(newItem.netID);

                if (player.TPlayer.miscEquips[index].netID != 0)
                {
                    player.TPlayer.miscEquips[index] = newItem;
                    NetMessage.SendData(5, -1, -1, NetworkText.Empty, player.TPlayer.whoAmI, slotId, player.TPlayer.miscEquips[index].prefix, player.TPlayer.miscEquips[index].stack);
                }
            }
            else if (slotId <
                NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.MiscEquipSlots
                + NetItem.MiscDyeSlots)
            {
                //93-98
                var index = slotId - (NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots
                    + NetItem.MiscEquipSlots);
                player.TPlayer.miscDyes[index].netDefaults(newItem.netID);

                if (player.TPlayer.miscDyes[index].netID != 0)
                {
                    player.TPlayer.miscDyes[index] = newItem;
                    NetMessage.SendData(5, -1, -1, NetworkText.Empty, player.TPlayer.whoAmI, slotId, player.TPlayer.miscDyes[index].prefix, player.TPlayer.miscDyes[index].stack);
                }
            }
        }

        /// <summary>
        /// Forces a clients SSC to a specific value
        /// </summary>
        /// <param name="on">Whether SSC is to be set to on or not</param>
        /// <param name="player"></param>
        internal void ForceClientSSC(Player player, bool on)
        {
            Main.ServerSideCharacter = on;
            NetMessage.SendData((int)PacketTypes.WorldInfo, player.Index, -1, NetworkText.FromLiteral(""));
        }

        /// <summary>
        /// Sends a raw packet built from base values to both fix the invincibility frames
        /// and provide a way to modify incoming damage.
        /// </summary>
        /// <param name="player">The TSPlayer object of the player to get hurt</param>
        /// <param name="hitDirection">The hit direction (left or right, -1 or 1)</param>
        /// <param name="damage">The amount of damage to deal to the player</param>
        internal void SendPlayerDamage(TSPlayer player, int hitDirection, int damage)
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

        /// <summary>
        /// Sends a raw packet that a player is dead to everyone
        /// </summary>
        /// <param name="player">The player who died</param>
        internal void SendPlayerDeath(TSPlayer player)
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
