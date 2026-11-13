using UnityEngine;

namespace HexQuickStackStorage.Services
{
    internal static class PlayerInventoryService
    {
        private const float LayoutRoundingTolerance = 0.01f;

        internal static bool TryGetStorageSize(Player player, out int width, out int height)
        {
            width = 0;
            height = 0;
            Inventory inventory = player?.GetInventory();

            if (inventory == null || InventoryGui.instance == null)
            {
                return false;
            }

            InventoryGrid grid = InventoryGui.instance.m_playerGrid;

            if (grid == null)
            {
                return false;
            }

            if (grid.m_gridRoot == null)
            {
                return false;
            }

            if (grid.m_elementPrefab == null)
            {
                return false;
            }

            if (grid.GetInventory() != inventory)
            {
                return false;
            }

            float cellSpacing = grid.m_elementSpace;

            if (cellSpacing <= 0f)
            {
                return false;
            }

            RectTransform slot = grid.m_elementPrefab.GetComponent<RectTransform>();

            if (slot == null)
            {
                return false;
            }

            // Count cells that fit in Valheim's player grid root, including a final
            // slot whose trailing spacing lies outside the rectangle.
            Rect gridBounds = grid.m_gridRoot.rect;
            width = CountCells(gridBounds.width, slot.rect.width, cellSpacing);
            height = CountCells(gridBounds.height, slot.rect.height, cellSpacing);
            width = Mathf.Clamp(width, 0, inventory.GetWidth());
            height = Mathf.Clamp(height, 0, inventory.GetHeight());

            return width > 0 && height > 0;
        }

        private static int CountCells(float extent, float slotSize, float spacing)
        {
            if (float.IsNaN(extent) || float.IsInfinity(extent))
            {
                return 0;
            }

            float trailingSpace = Mathf.Max(0f, spacing - slotSize);

            return Mathf.FloorToInt((extent + trailingSpace + LayoutRoundingTolerance) / spacing);
        }

        internal static bool IsStorageItem(Player player, ItemDrop.ItemData item)
        {
            if (item == null)
            {
                return false;
            }

            if (!TryGetStorageSize(player, out int width, out int height))
            {
                return false;
            }

            if (item.m_gridPos.x < 0 || item.m_gridPos.x >= width)
            {
                return false;
            }

            return item.m_gridPos.y >= 0 && item.m_gridPos.y < height;
        }
    }
}
