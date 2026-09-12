using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace HexQuickStackStorage.UI
{
    internal static class InventoryBorderRenderer
    {
        private const float BorderThickness = 2f;

        private static readonly FieldInfo ElementsField = AccessTools.Field(typeof(InventoryGrid), "m_elements");

        internal static void Refresh(
            InventoryGrid grid,
            string borderName,
            Color borderColor,
            Func<ItemDrop.ItemData, bool> shouldShowBorder
        )
        {
            if (grid == null || grid.GetInventory() == null || shouldShowBorder == null)
            {
                return;
            }

            Player player = Player.m_localPlayer;

            if (player == null || grid.GetInventory() != player.GetInventory())
            {
                return;
            }

            List<InventoryElement> elements = ElementsField?.GetValue(grid) as List<InventoryElement>;

            if (elements == null)
            {
                return;
            }

            int width = grid.GetInventory().GetWidth();

            foreach (InventoryElement element in elements)
            {
                if (element == null)
                {
                    continue;
                }

                SetBorderVisible(element, borderName, borderColor, false);
            }

            foreach (ItemDrop.ItemData item in grid.GetInventory().GetAllItems())
            {
                if (item == null || !shouldShowBorder(item))
                {
                    continue;
                }

                int index = item.m_gridPos.y * width + item.m_gridPos.x;

                if (index < 0 || index >= elements.Count)
                {
                    continue;
                }

                InventoryElement element = elements[index];

                if (element != null)
                {
                    SetBorderVisible(element, borderName, borderColor, true);
                }
            }
        }

        private static void SetBorderVisible(
            InventoryElement element,
            string borderName,
            Color borderColor,
            bool visible
        )
        {
            Transform existing = element.transform.Find(borderName);
            GameObject border = existing != null ? existing.gameObject : null;

            if (border == null && visible)
            {
                border = CreateBorder(element.transform, borderName, borderColor);
            }

            if (border != null)
            {
                border.SetActive(visible);
            }
        }

        private static GameObject CreateBorder(
            Transform parent,
            string borderName,
            Color borderColor
        )
        {
            GameObject border = new GameObject(borderName, typeof(RectTransform));
            RectTransform borderRect = border.GetComponent<RectTransform>();

            borderRect.SetParent(parent, false);
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            CreateEdge(border.transform, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -BorderThickness), Vector2.zero, borderColor);
            CreateEdge(border.transform, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, BorderThickness), borderColor);
            CreateEdge(border.transform, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(BorderThickness, 0f), borderColor);
            CreateEdge(border.transform, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-BorderThickness, 0f), Vector2.zero, borderColor);

            borderRect.SetAsLastSibling();

            return border;
        }

        private static void CreateEdge(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color borderColor
        )
        {
            GameObject edge = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rect = edge.GetComponent<RectTransform>();

            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Image image = edge.GetComponent<Image>();
            image.color = borderColor;
            image.raycastTarget = false;
        }
    }
}