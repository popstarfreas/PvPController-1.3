using System.Collections;
using System.Collections.Generic;
using Terraria;
using TShockAPI;

namespace PvPController
{
    internal static class Inventory
    {
        internal struct InventorySlot
        {
            public Item Item;
            public int SlotIndex;
        }

        internal static Item? GetItem(Terraria.Player player, int slotId)
        {
            Item? item = null;

            if (slotId < NetItem.InventorySlots)
            {
                // 0-58
                item = player.inventory[slotId];
            }
            else if (slotId < NetItem.InventorySlots + NetItem.ArmorSlots)
            {
                // 59-78
                var index = slotId - NetItem.InventorySlots;
                item = player.armor[index];
            }
            else if (slotId < NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots)
            {
                // 79-88
                var index = slotId - (NetItem.InventorySlots + NetItem.ArmorSlots);
                item = player.dye[index];
            }
            else if (slotId <
                NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.MiscEquipSlots)
            {
                // 89-93
                var index = slotId - (NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots);
                item = player.miscEquips[index];
            }
            else if (slotId <
                NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.MiscEquipSlots
                + NetItem.MiscDyeSlots)
            {
                // 93-98
                var index = slotId - (NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots
                    + NetItem.MiscEquipSlots);
                item = player.miscDyes[index];
            }

            return item;
        }

        internal static void SetItem(Terraria.Player player, int slotId, Item item)
        {
            if (slotId < NetItem.InventorySlots)
            {
                // 0-58
                player.inventory[slotId] = item;
            }
            else if (slotId < NetItem.InventorySlots + NetItem.ArmorSlots)
            {
                // 59-78
                var index = slotId - NetItem.InventorySlots;
                player.armor[index] = item;
            }
            else if (slotId < NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots)
            {
                // 79-88
                var index = slotId - (NetItem.InventorySlots + NetItem.ArmorSlots);
                player.dye[index] = item;
            }
            else if (slotId <
                NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.MiscEquipSlots)
            {
                // 89-93
                var index = slotId - (NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots);
                player.miscEquips[index] = item;
            }
            else if (slotId <
                NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.MiscEquipSlots
                + NetItem.MiscDyeSlots)
            {
                // 93-98
                var index = slotId - (NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots
                    + NetItem.MiscEquipSlots);
                player.miscDyes[index] = item;
            }
        }

        internal static IEnumerable<InventorySlot> AsIEnumerable(Terraria.Player player)
        {
            List<InventorySlot> inventory = new List<InventorySlot>();
            for (int i = 0; i <= PvPController.MAX_SLOT_ID; i++)
            {
                inventory.Add(new InventorySlot()
                {
                    Item = GetItem(player, i),
                    SlotIndex = i
                });
            }

            return inventory;
        }
    }
}
