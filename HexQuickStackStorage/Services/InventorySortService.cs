using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace HexQuickStackStorage
{
    internal static class InventorySortService
    {
        private static readonly MethodInfo ChangedMethod = AccessTools.Method(typeof(Inventory), "Changed", new[] { typeof(bool), typeof(bool) });
        private static readonly object[] ChangedArguments = { true, true };

        internal static void SortPlayerInventory()
        {
            Player player = Player.m_localPlayer;

            if (player == null)
            {
                return;
            }

            Inventory inventory = player.GetInventory();

            if (inventory == null)
            {
                return;
            }

            int vanillaHeight = GetVanillaInventoryHeight(player);

            Sort(inventory, vanillaHeight, true);
        }

        internal static void SortContainer(Container container)
        {
            if (container == null)
            {
                return;
            }

            Player player = Player.m_localPlayer;

            if (player == null)
            {
                return;
            }

            long playerId = Game.instance.GetPlayerProfile().GetPlayerID();

            if (!ContainerService.IsPlayerOwnedContainer(container, playerId))
            {
                return;
            }

            Inventory inventory = container.GetInventory();

            if (inventory == null)
            {
                return;
            }

            Sort(inventory, inventory.GetHeight(), false);
        }

        private static void Sort(Inventory inventory, int validHeight, bool preserveHotbar)
        {
            List<ItemDrop.ItemData> allItems = inventory.GetAllItems();

            if (allItems == null || allItems.Count <= 1)
            {
                return;
            }

            int width = inventory.GetWidth();

            if (width <= 0 || validHeight <= 0)
            {
                return;
            }

            List<ItemDrop.ItemData> itemsToSort = new List<ItemDrop.ItemData>(allItems.Count);
            HashSet<(int x, int y)> reservedSlots = new HashSet<(int x, int y)>();

            foreach (ItemDrop.ItemData item in allItems)
            {
                if (item == null)
                {
                    continue;
                }

                if (!IsValidSlot(item.m_gridPos, width, validHeight))
                {
                    continue;
                }

                if (item.m_equipped)
                {
                    reservedSlots.Add((item.m_gridPos.x, item.m_gridPos.y));
                    continue;
                }

                if (preserveHotbar && item.m_gridPos.y == 0)
                {
                    reservedSlots.Add((item.m_gridPos.x, item.m_gridPos.y));
                    continue;
                }

                itemsToSort.Add(item);
            }

            itemsToSort.Sort(CompareItems);

            int itemIndex = 0;

            for (int y = preserveHotbar ? 1 : 0; y < validHeight && itemIndex < itemsToSort.Count; y++)
            {
                for (int x = 0; x < width && itemIndex < itemsToSort.Count; x++)
                {
                    if (reservedSlots.Contains((x, y)))
                    {
                        continue;
                    }

                    ItemDrop.ItemData item = itemsToSort[itemIndex];
                    item.m_gridPos = new Vector2i(x, y);
                    itemIndex++;
                }
            }

            ChangedMethod?.Invoke(inventory, ChangedArguments);
        }

        private static int GetVanillaInventoryHeight(Player player)
        {
            if (player.TryGetUniqueKeyValue(Player.InventoryRowsKey, out string value) && int.TryParse(value, out int rows))
            {
                return Mathf.Clamp(rows, 0, 9);
            }

            return 4;
        }

        private static bool IsValidSlot(Vector2i position, int width, int height)
        {
            return position.x >= 0 && position.x < width && position.y >= 0 && position.y < height;
        }

        private static int CompareItems(ItemDrop.ItemData a, ItemDrop.ItemData b)
        {
            int result = a.m_shared.m_itemType.CompareTo(b.m_shared.m_itemType);

            if (result != 0)
            {
                return result;
            }

            result = string.Compare(a.m_shared.m_name, b.m_shared.m_name, StringComparison.Ordinal);

            if (result != 0)
            {
                return result;
            }

            result = b.m_quality.CompareTo(a.m_quality);

            if (result != 0)
            {
                return result;
            }

            return b.m_stack.CompareTo(a.m_stack);
        }
    }
}