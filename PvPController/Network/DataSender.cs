using TShockAPI;
using Terraria;
using Terraria.Localization;

namespace PvPController
{
    internal static class DataSender
    {
        /// <summary>
        /// Forces a players active health to a given value
        /// </summary>
        /// <param name="player">The player that is being updated</param>
        /// <param name="health">The new health value</param>
        internal static void SendClientHealth(Player player, int health)
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
        internal static void SendSlotUpdate(Player player, int slotId, Item newItem)
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
        internal static void ForceServerItem(Player player, int slotId, Item newItem)
        {
            Item? item = Inventory.GetItem(player.TPlayer, slotId);
            item.netDefaults(newItem.netID);
            if (item.netID != 0)
            {
                Inventory.SetItem(player.TPlayer, slotId, newItem);
                NetMessage.SendData(5, -1, -1, NetworkText.Empty, player.TPlayer.whoAmI, slotId, newItem.prefix, newItem.stack);
            }
        }

        /// <summary>
        /// Forces a clients SSC to a specific value if tshock config set to false
        /// </summary>
        /// <param name="on">Whether SSC is to be set to on or not</param>
        /// <param name="player"></param>
        internal static void ForceClientSSC(Player player, bool on)
        {
            if (!TShock.ServerSideCharacterConfig.Enabled)
            {
                Main.ServerSideCharacter = on;
                NetMessage.SendData((int)PacketTypes.WorldInfo, player.Index, -1, NetworkText.FromLiteral(""));
            }
        }

        /// <summary>
        /// Sends a raw packet built from base values to both fix the invincibility frames
        /// and provide a way to modify incoming damage.
        /// </summary>
        /// <param name="player">The TSPlayer object of the player to get hurt</param>
        /// <param name="hitDirection">The hit direction (left or right, -1 or 1)</param>
        /// <param name="damage">The amount of damage to deal to the player</param>
        internal static void SendPlayerDamage(TSPlayer player, int hitDirection, int damage)
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
        internal static void SendPlayerDeath(TSPlayer player)
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
