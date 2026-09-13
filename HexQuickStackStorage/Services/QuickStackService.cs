using HexQuickStackStorage.Services;
using System.Collections.Generic;
using UnityEngine;

namespace HexQuickStackStorage
{
    internal static class QuickStackService
    {
        private const int DefaultVanillaInventoryRows = 4;

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
                if (QuickStackIntoContainer(container, quickStackableItems, playerInventory))
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

            if (QuickStackIntoContainer(container, quickStackableItems, playerInventory))
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

        private static bool QuickStackIntoContainer(Container container, List<ItemDrop.ItemData> quickStackableItems, Inventory playerInventory)
        {
            if (container == null)
            {
                return false;
            }

            ZNetView nview = container.GetComponent<ZNetView>();

            if (nview == null || !nview.IsValid())
            {
                return false;
            }

            if (IsContainerInUse(container, nview))
            {
                return false;
            }

            nview.ClaimOwnership();

            Inventory containerInventory = container.GetInventory();

            if (containerInventory == null)
            {
                return false;
            }

            SetContainerInUse(nview, true);

            try
            {
                return QuickStackItems(quickStackableItems, playerInventory, containerInventory);
            }
            finally
            {
                SetContainerInUse(nview, false);
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

                if (!containerInventory.ContainsItemByName(item.m_shared.m_name))
                {
                    continue;
                }

                int originalStack = item.m_stack;

                bool fullyAdded = containerInventory.AddItem(item);

                if (fullyAdded)
                {
                    playerInventory.RemoveItem(item);
                    quickStackableItems.RemoveAt(i);

                    itemsMoved = true;
                    continue;
                }

                if (item.m_stack < originalStack)
                {
                    InventoryOperationsService.NotifyChanged(playerInventory);

                    itemsMoved = true;
                }
            }

            return itemsMoved;
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
    }
}