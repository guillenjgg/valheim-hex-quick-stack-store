using HexQuickStackStorage.Services;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace HexQuickStackStorage
{
    internal static class QuickStackService
    {
        private const float StackRequestTimeout = 10f;

        private static readonly FieldInfo ContainerNViewField = typeof(Container).GetField("m_nview", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly Dictionary<Container, StackRequest> PendingRequests = new Dictionary<Container, StackRequest>();

        private sealed class StackRequest
        {
            internal Player Player;
            internal Inventory Inventory;
            internal float StartedAt;
        }

        internal static void QuickStack(Player player)
        {
            List<Container> expiredRequests = new List<Container>();

            foreach (var request in PendingRequests)
            {
                if (request.Key == null)
                {
                    expiredRequests.Add(request.Key);

                    continue;
                }

                if (request.Value.Player != player)
                {
                    expiredRequests.Add(request.Key);

                    continue;
                }

                float requestAge = Time.realtimeSinceStartup - request.Value.StartedAt;

                if (requestAge >= StackRequestTimeout)
                {
                    expiredRequests.Add(request.Key);
                }
            }

            foreach (Container expired in expiredRequests)
            {
                PendingRequests.Remove(expired);
            }

            Inventory inventory = player?.GetInventory();

            if (inventory == null)
            {
                return;
            }

            if (GetQuickStackableItems(player, inventory).Count == 0)
            {
                return;
            }

            foreach (Container container in ContainerService.GetNearbyContainers(player))
            {
                ZNetView nview = GetContainerNView(container);

                if (nview == null)
                {
                    continue;
                }

                if (!nview.IsValid())
                {
                    continue;
                }

                if (IsContainerInUse(container, nview))
                {
                    continue;
                }

                if (PendingRequests.TryGetValue(container, out StackRequest pending))
                {
                    float pendingRequestAge = Time.realtimeSinceStartup - pending.StartedAt;

                    if (pending.Player == player && pendingRequestAge < StackRequestTimeout)
                    {
                        continue;
                    }
                }

                PendingRequests[container] = new StackRequest
                {
                    Player = player,
                    Inventory = inventory,
                    StartedAt = Time.realtimeSinceStartup
                };
                // The current owner checks access and transfers ownership before replying.
                container.StackAll();
            }
        }

        internal static bool CompleteNearbyStackRequest(Container container, bool granted)
        {
            if (!PendingRequests.TryGetValue(container, out StackRequest request))
            {
                return false;
            }

            PendingRequests.Remove(container);

            if (!granted)
            {
                return true;
            }

            Player player = Player.m_localPlayer;

            if (player == null || player != request.Player)
            {
                return true;
            }

            if (player.GetInventory() != request.Inventory)
            {
                return true;
            }

            float requestAge = Time.realtimeSinceStartup - request.StartedAt;

            if (requestAge >= StackRequestTimeout)
            {
                return true;
            }

            ZNetView nview = GetContainerNView(container);

            if (nview == null || !nview.IsValid())
            {
                return true;
            }

            if (!nview.IsOwner())
            {
                return true;
            }

            if (IsContainerInUse(container, nview))
            {
                return true;
            }

            if (!ContainerService.CanUseContainer(container))
            {
                return true;
            }

            float distanceToContainer = Vector3.Distance(player.transform.position, container.transform.position);

            if (distanceToContainer > Plugin.SearchRadius)
            {
                return true;
            }

            // Rebuild the item list now: the player may have moved, equipped, or favorited items while waiting.
            QuickStack(player, container);

            return true;
        }

        internal static void ClearPendingRequests()
        {
            PendingRequests.Clear();
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

            List<ItemDrop.ItemData> quickStackableItems = GetQuickStackableItems(player, playerInventory);

            if (quickStackableItems.Count == 0)
            {
                return;
            }

            if (QuickStackIntoContainer(container, quickStackableItems, playerInventory))
            {
                player.Message(MessageHud.MessageType.TopLeft, "Items auto stacked", 0, null, false);
            }
        }

        private static bool QuickStackIntoContainer(Container container, List<ItemDrop.ItemData> quickStackableItems, Inventory playerInventory)
        {
            ZNetView nview = GetContainerNView(container);

            if (nview == null)
            {
                return false;
            }

            if (!nview.IsValid())
            {
                return false;
            }

            if (!nview.IsOwner())
            {
                return false;
            }

            Inventory containerInventory = container.GetInventory();

            return containerInventory != null && QuickStackItems(quickStackableItems, playerInventory, containerInventory);
        }

        private static List<ItemDrop.ItemData> GetQuickStackableItems(Player player, Inventory playerInventory)
        {
            var items = new List<ItemDrop.ItemData>();

            if (!PlayerInventoryService.TryGetStorageSize(player, out int storageWidth, out int storageHeight))
            {
                return items;
            }

            foreach (ItemDrop.ItemData item in playerInventory.GetAllItems())
            {
                if (item == null || item.m_shared == null)
                {
                    continue;
                }

                if (item.m_gridPos.x < 0 || item.m_gridPos.x >= storageWidth)
                {
                    continue;
                }

                var isItemOutsideStorageArea = item.m_gridPos.y < 0 || item.m_gridPos.y >= storageHeight;

                if (isItemOutsideStorageArea)
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
                bool containsTrophy = isTrophy && containerInventory.GetAllItems().Exists(IsTrophy);

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

        private static bool IsTrophy(ItemDrop.ItemData item)
        {
            return item?.m_shared?.m_itemType == ItemDrop.ItemData.ItemType.Trophy;
        }
    }
}
