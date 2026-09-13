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

                QuickStackIntoContainer(
                    player,
                    playerInventory,
                    containerInventory,
                    vanillaHeight
                );
            }
        }

        private static void QuickStackIntoContainer(
            Player player,
            Inventory playerInventory,
            Inventory containerInventory,
            int vanillaHeight
        )
        {
            List<ItemDrop.ItemData> items =
                new List<ItemDrop.ItemData>(
                    playerInventory.GetAllItems()
                );

            foreach (ItemDrop.ItemData item in items)
            {
                if (item == null)
                {
                    continue;
                }

                if (item.m_gridPos.y < 0 ||
                    item.m_gridPos.y >= vanillaHeight)
                {
                    continue;
                }

                if (ItemStateService.IsHotbarItem(item))
                {
                    continue;
                }

                if (player.IsItemEquiped(item))
                {
                    continue;
                }

                if (FavoriteService.IsFavorite(item))
                {
                    continue;
                }

                if (!ContainerHasMatchingItem(
                    containerInventory,
                    item
                ))
                {
                    continue;
                }

                int originalStack = item.m_stack;

                bool fullyAdded =
                    containerInventory.AddItem(item);

                if (fullyAdded)
                {
                    playerInventory.RemoveItem(item);
                    continue;
                }

                if (item.m_stack < originalStack)
                {
                    playerInventory.Changed();
                }
            }
        }

        private static bool ContainerHasMatchingItem(
            Inventory containerInventory,
            ItemDrop.ItemData sourceItem
        )
        {
            List<ItemDrop.ItemData> containerItems =
                containerInventory.GetAllItems();

            foreach (ItemDrop.ItemData containerItem in containerItems)
            {
                if (containerItem == null ||
                    containerItem.m_shared == null)
                {
                    continue;
                }

                if (containerItem.m_shared.m_name ==
                    sourceItem.m_shared.m_name)
                {
                    return true;
                }
            }

            return false;
        }

        private static int GetVanillaInventoryHeight(
            Player player
        )
        {
            if (player.TryGetUniqueKeyValue(
                    Player.InventoryRowsKey,
                    out string value
                ) &&
                int.TryParse(
                    value,
                    out int rows
                ))
            {
                return Mathf.Clamp(
                    rows,
                    0,
                    9
                );
            }

            return 4;
        }
    }
}