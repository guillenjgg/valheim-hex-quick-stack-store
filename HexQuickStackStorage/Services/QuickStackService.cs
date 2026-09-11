using System.Collections.Generic;
using UnityEngine;

namespace HexQuickStackStorage
{
    internal static class QuickStackService
    {
        internal static void QuickStack(Player player)
        {
            if (player == null)
            {
                return;
            }

            Inventory playerInventory = player.GetInventory();

            if (playerInventory == null)
            {
                return;
            }

            int vanillaHeight = GetVanillaInventoryHeight(player);
            List<Container> containers = ContainerService.GetNearbyContainers(player);
            int beforeCount = playerInventory.CountItems(null, -1, true);

            foreach (Container container in containers)
            {
                if (container == null)
                {
                    continue;
                }

                Inventory containerInventory = container.GetInventory();

                if (containerInventory == null)
                {
                    continue;
                }

                QuickStackIntoContainer(player, playerInventory, containerInventory, vanillaHeight);
            }

            int afterCount = playerInventory.CountItems(null, -1, true);
            int movedCount = beforeCount - afterCount;
        }

        private static void QuickStackIntoContainer(Player player, Inventory playerInventory, Inventory containerInventory, int vanillaHeight)
        {
            List<ItemDrop.ItemData> items = new List<ItemDrop.ItemData>(playerInventory.GetAllItems());

            foreach (ItemDrop.ItemData item in items)
            {
                if (item == null)
                {
                    continue;
                }

                if (item.m_gridPos.y < 0 || item.m_gridPos.y >= vanillaHeight)
                {
                    continue;
                }

                if (item.m_gridPos.y == 0)
                {
                    continue;
                }

                if (player.IsItemEquiped(item))
                {
                    continue;
                }

                if (!ContainerHasMatchingItem(containerInventory, item))
                {
                    continue;
                }

                if (!containerInventory.CanAddItem(item))
                {
                    continue;
                }

                if (containerInventory.AddItem(item))
                {
                    playerInventory.RemoveItem(item);
                }
            }
        }

        private static bool ContainerHasMatchingItem(Inventory containerInventory, ItemDrop.ItemData sourceItem)
        {
            List<ItemDrop.ItemData> containerItems = containerInventory.GetAllItems();

            foreach (ItemDrop.ItemData containerItem in containerItems)
            {
                if (containerItem == null)
                {
                    continue;
                }

                if (containerItem.m_shared.m_name == sourceItem.m_shared.m_name)
                {
                    return true;
                }
            }

            return false;
        }

        private static int GetVanillaInventoryHeight(Player player)
        {
            if (player.TryGetUniqueKeyValue(Player.InventoryRowsKey, out string value) && int.TryParse(value, out int rows))
            {
                return Mathf.Clamp(rows, 0, 9);
            }

            return 4;
        }
    }
}