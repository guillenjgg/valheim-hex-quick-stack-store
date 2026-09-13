using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace HexQuickStackStorage
{
    internal static class TrashService
    {
        private const string JunkFileName = "HexQuickStackStorage.junk.txt";

        private static readonly HashSet<string> JunkItemNames = new HashSet<string>(StringComparer.Ordinal);

        private static readonly FieldInfo DragItemField = AccessTools.Field(typeof(InventoryGui), "m_dragItem");
        private static readonly FieldInfo DragInventoryField = AccessTools.Field(typeof(InventoryGui), "m_dragInventory");
        private static readonly FieldInfo DragAmountField = AccessTools.Field(typeof(InventoryGui), "m_dragAmount");
        private static readonly MethodInfo SetupDragItemMethod = AccessTools.Method(typeof(InventoryGui), "SetupDragItem");

        private static string JunkFilePath => Path.Combine(BepInEx.Paths.ConfigPath, JunkFileName);

        internal static void Initialize()
        {
            LoadJunkItems();
        }

        internal static bool IsMarked(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null)
            {
                return false;
            }

            return JunkItemNames.Contains(item.m_shared.m_name);
        }

        internal static void Mark(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null)
            {
                return;
            }

            if (JunkItemNames.Add(item.m_shared.m_name))
            {
                SaveJunkItems();
            }
        }

        internal static void Unmark(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null)
            {
                return;
            }

            if (JunkItemNames.Remove(item.m_shared.m_name))
            {
                SaveJunkItems();
            }
        }

        internal static void DeleteDraggedItem(InventoryGui inventoryGui, Player player)
        {
            if (inventoryGui == null || player == null)
            {
                return;
            }

            ItemDrop.ItemData item = DragItemField?.GetValue(inventoryGui) as ItemDrop.ItemData;
            Inventory sourceInventory = DragInventoryField?.GetValue(inventoryGui) as Inventory;

            if (item == null || sourceInventory == null)
            {
                return;
            }

            Inventory playerInventory = player.GetInventory();

            if (sourceInventory != playerInventory)
            {
                return;
            }

            if (!IsVanillaInventoryItem(player, item))
            {
                return;
            }

            if (!ItemStateService.CanDelete(item))
            {
                ShowProtectedItemMessage(player);
                return;
            }

            int dragAmount = DragAmountField != null ? (int)DragAmountField.GetValue(inventoryGui) : 1;
            int amount = Mathf.Min(dragAmount, item.m_stack);

            if (!sourceInventory.RemoveItem(item, amount))
            {
                return;
            }

            SetupDragItemMethod?.Invoke(inventoryGui, new object[] { null, null, 1 });
        }

        internal static void DeleteMarkedItems(Player player)
        {
            if (player == null)
            {
                return;
            }

            Inventory inventory = player.GetInventory();

            if (inventory == null)
            {
                return;
            }

            List<ItemDrop.ItemData> items = new List<ItemDrop.ItemData>(inventory.GetAllItems());
            bool protectedItemSkipped = false;

            foreach (ItemDrop.ItemData item in items)
            {
                if (item == null || item.m_shared == null)
                {
                    continue;
                }

                if (!IsMarked(item))
                {
                    continue;
                }

                if (!IsVanillaInventoryItem(player, item))
                {
                    continue;
                }

                if (!ItemStateService.CanDelete(item))
                {
                    protectedItemSkipped = true;
                    continue;
                }

                inventory.RemoveItem(item);
            }

            if (protectedItemSkipped)
            {
                ShowProtectedItemMessage(player);
            }
        }

        internal static bool IsVanillaInventoryItem(Player player, ItemDrop.ItemData item)
        {
            if (player == null || item == null)
            {
                return false;
            }

            int vanillaHeight = GetVanillaInventoryHeight(player);

            return item.m_gridPos.y >= 0 && item.m_gridPos.y < vanillaHeight;
        }

        private static int GetVanillaInventoryHeight(Player player)
        {
            if (player.TryGetUniqueKeyValue(Player.InventoryRowsKey, out string value) && int.TryParse(value, out int rows))
            {
                return Mathf.Clamp(rows, 0, 9);
            }

            return 4;
        }

        private static void LoadJunkItems()
        {
            JunkItemNames.Clear();

            if (!File.Exists(JunkFilePath))
            {
                return;
            }

            foreach (string line in File.ReadAllLines(JunkFilePath))
            {
                string itemName = line.Trim();

                if (!string.IsNullOrEmpty(itemName))
                {
                    JunkItemNames.Add(itemName);
                }
            }
        }

        private static void SaveJunkItems()
        {
            Directory.CreateDirectory(BepInEx.Paths.ConfigPath);

            List<string> itemNames = new List<string>(JunkItemNames);
            itemNames.Sort(StringComparer.Ordinal);

            File.WriteAllLines(JunkFilePath, itemNames);
        }

        private static void ShowProtectedItemMessage(Player player)
        {
            player.Message(
                MessageHud.MessageType.Center,
                "Can't delete equipped, hotbar, or favorited items.",
                0,
                null
            );
        }
    }
}