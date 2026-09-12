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
        private static readonly object[] ChangedArguments = { false, false };

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

        private static void Sort(Inventory inventory, int validHeight, bool isPlayerInventory)
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
                if (item == null || item.m_shared == null)
                {
                    continue;
                }

                if (!IsValidSlot(item.m_gridPos, width, validHeight))
                {
                    continue;
                }

                if (isPlayerInventory && item.m_equipped)
                {
                    reservedSlots.Add((item.m_gridPos.x, item.m_gridPos.y));
                    continue;
                }

                if (isPlayerInventory && ItemStateService.IsHotbarItem(item))
                {
                    reservedSlots.Add((item.m_gridPos.x, item.m_gridPos.y));
                    continue;
                }

                if (isPlayerInventory && FavoriteService.IsFavorite(item))
                {
                    reservedSlots.Add((item.m_gridPos.x, item.m_gridPos.y));
                    continue;
                }

                itemsToSort.Add(item);
            }

            itemsToSort.Sort(CompareItems);

            ConsolidateStacks(inventory, itemsToSort);

            itemsToSort.Sort(CompareItems);

            int itemIndex = 0;
            int startRow = isPlayerInventory ? 1 : 0;

            for (int y = startRow; y < validHeight && itemIndex < itemsToSort.Count; y++)
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

        private static void ConsolidateStacks(Inventory inventory, List<ItemDrop.ItemData> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                ItemDrop.ItemData targetItem = items[i];

                if (targetItem == null || targetItem.m_shared == null || targetItem.m_shared.m_maxStackSize <= 1)
                {
                    continue;
                }

                for (int j = i + 1; j < items.Count;)
                {
                    ItemDrop.ItemData sourceItem = items[j];

                    if (!CanStackTogether(targetItem, sourceItem))
                    {
                        j++;
                        continue;
                    }

                    int availableSpace = targetItem.m_shared.m_maxStackSize - targetItem.m_stack;

                    if (availableSpace <= 0)
                    {
                        break;
                    }

                    int amountToMove = Math.Min(availableSpace, sourceItem.m_stack);

                    targetItem.m_stack += amountToMove;
                    sourceItem.m_stack -= amountToMove;

                    if (sourceItem.m_stack <= 0)
                    {
                        inventory.RemoveItem(sourceItem);
                        items.RemoveAt(j);
                        continue;
                    }

                    j++;
                }
            }
        }

        private static bool CanStackTogether(ItemDrop.ItemData targetItem, ItemDrop.ItemData sourceItem)
        {
            if (targetItem == null || sourceItem == null || targetItem.m_shared == null || sourceItem.m_shared == null)
            {
                return false;
            }

            if (targetItem == sourceItem)
            {
                return false;
            }

            if (targetItem.m_shared.m_name != sourceItem.m_shared.m_name)
            {
                return false;
            }

            if (targetItem.m_quality != sourceItem.m_quality)
            {
                return false;
            }

            if (targetItem.m_worldLevel != sourceItem.m_worldLevel)
            {
                return false;
            }

            return targetItem.m_stack < targetItem.m_shared.m_maxStackSize;
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

        private static int CompareItems(ItemDrop.ItemData firstItem, ItemDrop.ItemData secondItem)
        {
            int result = firstItem.m_shared.m_itemType.CompareTo(secondItem.m_shared.m_itemType);

            if (result != 0)
            {
                return result;
            }

            result = string.Compare(firstItem.m_shared.m_name, secondItem.m_shared.m_name, StringComparison.Ordinal);

            if (result != 0)
            {
                return result;
            }

            result = secondItem.m_quality.CompareTo(firstItem.m_quality);

            if (result != 0)
            {
                return result;
            }

            return secondItem.m_stack.CompareTo(firstItem.m_stack);
        }
    }
}