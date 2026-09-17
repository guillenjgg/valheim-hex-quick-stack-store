using HexQuickStackStorage.Services;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace HexQuickStackStorage
{
    internal static class QuickStackService
    {
        private const int DefaultVanillaInventoryRows = 4;

        private static readonly FieldInfo ContainerNViewField = typeof(Container).GetField("m_nview", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

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

            List<ItemDrop.ItemData> quickStackableItems = GetQuickStackableItems(player, playerInventory, vanillaHeight);

            if (quickStackableItems.Count == 0)
            {
                return;
            }

            List<Container> containers = ContainerService.GetNearbyContainers(player);

            bool itemsMoved = false;

            foreach (Container container in containers)
            {
                if (QuickStackIntoContainer(container, quickStackableItems, playerInventory, false))
                {
                    itemsMoved = true;
                }

                if (quickStackableItems.Count == 0)
                {
                    break;
                }
            }

            if (itemsMoved)
            {
                player.Message(
                    MessageHud.MessageType.TopLeft,
                    "Items auto stacked",
                    0,
                    null,
                    false
                );
            }
        }

        internal static void QuickStack(Player player, Container container)
        {
            if (player == null || container == null)
            {
                return;
            }

            Inventory playerInventory = player.GetInventory();

            if (playerInventory == null)
            {
                return;
            }

            int vanillaHeight = GetVanillaInventoryHeight(player);

            List<ItemDrop.ItemData> quickStackableItems = GetQuickStackableItems(player, playerInventory, vanillaHeight);

            if (quickStackableItems.Count == 0)
            {
                return;
            }

            if (QuickStackIntoContainer(container, quickStackableItems, playerInventory, true))
            {
                player.Message(
                    MessageHud.MessageType.TopLeft,
                    "Items auto stacked",
                    0,
                    null,
                    false
                );
            }
        }

        private static bool QuickStackIntoContainer(Container container, List<ItemDrop.ItemData> quickStackableItems, Inventory playerInventory, bool allowInUse)
        {
            if (container == null)
            {
                return false;
            }

            ZNetView nview = GetContainerNView(container);

            if (nview == null || !nview.IsValid())
            {
                return false;
            }

            if (!allowInUse && IsContainerInUse(container, nview))
            {
                return false;
            }

            nview.ClaimOwnership();

            Inventory containerInventory = container.GetInventory();

            if (containerInventory == null)
            {
                return false;
            }

            bool setInUse = !allowInUse;

            if (setInUse)
            {
                SetContainerInUse(nview, true);
            }

            try
            {
                return QuickStackItems(quickStackableItems, playerInventory, containerInventory);
            }
            finally
            {
                if (setInUse)
                {
                    SetContainerInUse(nview, false);
                }
            }
        }

        private static List<ItemDrop.ItemData> GetQuickStackableItems(Player player, Inventory playerInventory, int vanillaHeight)
        {
            var items = new List<ItemDrop.ItemData>();

            foreach (ItemDrop.ItemData item in playerInventory.GetAllItems())
            {
                if (item == null)
                {
                    continue;
                }

                var isItemOutsideVanillaInventoryArea = item.m_gridPos.y < 0 || item.m_gridPos.y >= vanillaHeight;

                if (isItemOutsideVanillaInventoryArea)
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

                if (IsTrophy(item) && !Plugin.EnableAutoStoreTrophies)
                {
                    continue;
                }

                items.Add(item);
            }

            return items;
        }

        private static bool QuickStackItems(List<ItemDrop.ItemData> quickStackableItems, Inventory playerInventory, Inventory containerInventory)
        {
            bool itemsMoved = false;

            for (int i = quickStackableItems.Count - 1; i >= 0; i--)
            {
                ItemDrop.ItemData item = quickStackableItems[i];

                if (item == null)
                {
                    quickStackableItems.RemoveAt(i);
                    continue;
                }

                bool isTrophy = IsTrophy(item);
                bool containsMatchingItem = containerInventory.ContainsItemByName(item.m_shared.m_name);

                bool containsTrophy = isTrophy && containerInventory
                    .GetAllItems()
                    .Exists(IsTrophy);

                if (!containsMatchingItem && !containsTrophy)
                {
                    continue;
                }

                while (item.m_stack > 0)
                {
                    ItemDrop.ItemData containerItem = InventoryOperationsService.FindFreeStackItem(containerInventory, item);

                    if (containerItem == null)
                    {
                        break;
                    }

                    int availableStackSpace = containerItem.m_shared.m_maxStackSize - containerItem.m_stack;
                    int amountToMove = Mathf.Min(item.m_stack, availableStackSpace);

                    containerItem.m_stack += amountToMove;
                    item.m_stack -= amountToMove;
                    itemsMoved = true;
                }

                while (item.m_stack > 0)
                {
                    Vector2i emptySlot = InventoryOperationsService.FindEmptySlot(containerInventory, item);

                    if (emptySlot.x < 0)
                    {
                        break;
                    }

                    int amountToMove = Mathf.Min(item.m_stack, item.m_shared.m_maxStackSize);

                    ItemDrop.ItemData newStack = item.Clone();
                    newStack.m_stack = amountToMove;
                    newStack.m_gridPos = emptySlot;

                    containerInventory.GetAllItems().Add(newStack);

                    item.m_stack -= amountToMove;
                    itemsMoved = true;
                }

                if (item.m_stack == 0)
                {
                    playerInventory.RemoveItem(item);
                    quickStackableItems.RemoveAt(i);
                }
            }

            if (itemsMoved)
            {
                InventoryOperationsService.NotifyChanged(playerInventory);
                InventoryOperationsService.NotifyChanged(containerInventory);
            }

            return itemsMoved;
        }

        private static ZNetView GetContainerNView(Container container)
        {
            if (container == null || ContainerNViewField == null)
            {
                return null;
            }

            return ContainerNViewField.GetValue(container) as ZNetView;
        }

        private static bool IsContainerInUse(Container container, ZNetView nview)
        {
            if (container.IsInUse() || (container.m_wagon != null && container.m_wagon.InUse()))
            {
                return true;
            }

            ZDO zdo = nview.GetZDO();

            if (zdo == null)
            {
                return false;
            }

            return zdo.GetInt(ZDOVars.s_inUse, 0) == 1;
        }

        private static void SetContainerInUse(ZNetView nview, bool isInUse)
        {
            ZDO zdo = nview.GetZDO();

            if (zdo == null)
            {
                return;
            }

            zdo.Set(ZDOVars.s_inUse, isInUse ? 1 : 0);
            ZDOMan.instance.ForceSendZDO(ZNet.GetUID(), zdo.m_uid);
        }

        private static int GetVanillaInventoryHeight(Player player)
        {
            var inventoryRows = player.TryGetUniqueKeyValue(Player.InventoryRowsKey, out string value);

            if (inventoryRows && int.TryParse(value, out int rows))
            {
                return Mathf.Clamp(rows, 0, 9);
            }

            return DefaultVanillaInventoryRows;
        }

        private static bool IsTrophy(ItemDrop.ItemData item)
        {
            return item?.m_shared?.m_itemType == ItemDrop.ItemData.ItemType.Trophy;
        }
    }
}