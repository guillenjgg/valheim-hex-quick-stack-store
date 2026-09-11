using HarmonyLib;
using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HexQuickStackStorage
{
    internal static class InventoryUiController
    {
        private const string SortButtonName = "HexSortButton";
        private const string QuickStackButtonName = "HexQuickStackButton";
        private const string TrashButtonName = "HexTrashButton";

        private const float ButtonSize = 36f;
        private const float ButtonSpacing = 4f;
        private const float TrashIconSize = 32f;
        private const float TrashIconXOffset = 4f;
        private const float TrashIconYOffset = 0f;

        private static readonly FieldInfo CurrentContainerField = AccessTools.Field(typeof(InventoryGui), "m_currentContainer");
        private static readonly FieldInfo DragItemField = AccessTools.Field(typeof(InventoryGui), "m_dragItem");

        private static InventoryGui _inventoryGui;

        internal static void Initialize(InventoryGui inventoryGui)
        {
            if (inventoryGui == null)
            {
                return;
            }

            _inventoryGui = inventoryGui;

            CreateButtons();
        }

        private static void CreateButtons()
        {
            if (_inventoryGui == null || _inventoryGui.m_player == null || _inventoryGui.m_takeAllButton == null)
            {
                return;
            }

            Button nativeButton = _inventoryGui.m_takeAllButton;

            CreateActionButton(nativeButton, SortButtonName, "S", 0, OnSortClicked);
            CreateActionButton(nativeButton, QuickStackButtonName, "Q", 1, OnQuickStackClicked);
            CreateTrashButton(OnTrashClicked);
        }

        private static Button CreateActionButton(Button template, string buttonName, string text, int index, UnityEngine.Events.UnityAction onClick)
        {
            Transform existing = _inventoryGui.m_player.transform.Find(buttonName);

            if (existing != null)
            {
                return existing.GetComponent<Button>();
            }

            GameObject buttonObject = UnityEngine.Object.Instantiate(template.gameObject, _inventoryGui.m_player.transform);
            buttonObject.name = buttonName;

            Button button = buttonObject.GetComponent<Button>();

            if (button == null)
            {
                UnityEngine.Object.Destroy(buttonObject);
                return null;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);

            SetButtonText(buttonObject, text);
            StyleActionButton(buttonObject);
            PositionActionButton(buttonObject, index);

            buttonObject.SetActive(true);

            return button;
        }

        private static Button CreateTrashButton(UnityEngine.Events.UnityAction onClick)
        {
            Transform existing = FindChildRecursive(_inventoryGui.transform, TrashButtonName);

            if (existing != null)
            {
                return existing.GetComponent<Button>();
            }

            RectTransform armorTab = FindStatusTab("armor");
            RectTransform weightTab = FindStatusTab("weight");

            if (armorTab == null || weightTab == null)
            {
                return null;
            }

            GameObject trashObject = UnityEngine.Object.Instantiate(weightTab.gameObject, weightTab.parent);
            trashObject.name = TrashButtonName;

            RectTransform trashRect = trashObject.GetComponent<RectTransform>();

            Transform weightIcon = trashObject.transform.Find("weight_icon");
            Transform weightText = trashObject.transform.Find("weight_text");

            if (weightText != null)
            {
                weightText.gameObject.SetActive(false);
            }

            if (weightIcon != null)
            {
                weightIcon.gameObject.SetActive(false);
            }

            CreateTrashIcon(trashObject.transform);

            Button button = trashObject.GetComponent<Button>();

            if (button == null)
            {
                button = trashObject.AddComponent<Button>();
            }

            Image rootImage = trashObject.GetComponent<Image>();

            if (rootImage != null)
            {
                button.targetGraphic = rootImage;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);

            PositionTrashButton(trashRect, armorTab, weightTab);

            int nativeSiblingIndex = Mathf.Min(armorTab.GetSiblingIndex(), weightTab.GetSiblingIndex());
            trashRect.SetSiblingIndex(nativeSiblingIndex);

            trashObject.SetActive(true);

            return button;
        }

        private static void CreateTrashIcon(Transform parent)
        {
            GameObject iconRoot = new GameObject("TrashIcon", typeof(RectTransform));
            RectTransform iconRootRect = iconRoot.GetComponent<RectTransform>();

            iconRootRect.SetParent(parent, false);
            iconRootRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRootRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRootRect.pivot = new Vector2(0.5f, 0.5f);
            iconRootRect.sizeDelta = new Vector2(TrashIconSize, TrashIconSize);
            iconRootRect.anchoredPosition = new Vector2(TrashIconXOffset, TrashIconYOffset);

            GameObject body = CreateIconPart(iconRoot.transform, "Body");
            RectTransform bodyRect = body.GetComponent<RectTransform>();

            bodyRect.anchorMin = new Vector2(0.20f, 0.10f);
            bodyRect.anchorMax = new Vector2(0.80f, 0.70f);
            bodyRect.offsetMin = Vector2.zero;
            bodyRect.offsetMax = Vector2.zero;

            GameObject lid = CreateIconPart(iconRoot.transform, "Lid");
            RectTransform lidRect = lid.GetComponent<RectTransform>();

            lidRect.anchorMin = new Vector2(0.10f, 0.74f);
            lidRect.anchorMax = new Vector2(0.90f, 0.84f);
            lidRect.offsetMin = Vector2.zero;
            lidRect.offsetMax = Vector2.zero;

            GameObject handle = CreateIconPart(iconRoot.transform, "Handle");
            RectTransform handleRect = handle.GetComponent<RectTransform>();

            handleRect.anchorMin = new Vector2(0.36f, 0.85f);
            handleRect.anchorMax = new Vector2(0.64f, 0.96f);
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;

            CreateTrashSlot(iconRoot.transform, 0.35f);
            CreateTrashSlot(iconRoot.transform, 0.50f);
            CreateTrashSlot(iconRoot.transform, 0.65f);
        }

        private static GameObject CreateIconPart(Transform parent, string name)
        {
            GameObject part = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rect = part.GetComponent<RectTransform>();

            rect.SetParent(parent, false);

            Image image = part.GetComponent<Image>();
            image.color = new Color(0.72f, 0.72f, 0.72f, 1f);
            image.raycastTarget = false;

            return part;
        }

        private static void CreateTrashSlot(Transform parent, float x)
        {
            GameObject slot = new GameObject("Slot", typeof(RectTransform), typeof(Image));
            RectTransform rect = slot.GetComponent<RectTransform>();

            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(x, 0.18f);
            rect.anchorMax = new Vector2(x, 0.60f);
            rect.sizeDelta = new Vector2(2f, 0f);

            Image image = slot.GetComponent<Image>();
            image.color = new Color(0.20f, 0.20f, 0.20f, 1f);
            image.raycastTarget = false;
        }

        private static void PositionTrashButton(RectTransform trashRect, RectTransform armorRect, RectTransform weightRect)
        {
            if (trashRect == null || armorRect == null || weightRect == null)
            {
                return;
            }

            trashRect.anchorMin = weightRect.anchorMin;
            trashRect.anchorMax = weightRect.anchorMax;
            trashRect.pivot = weightRect.pivot;
            trashRect.sizeDelta = weightRect.sizeDelta;

            Vector3 middleWorldPosition = (armorRect.position + weightRect.position) * 0.5f;
            trashRect.position = middleWorldPosition;
        }

        private static RectTransform FindStatusTab(string namePart)
        {
            RectTransform[] rectTransforms = _inventoryGui.GetComponentsInChildren<RectTransform>(true);

            foreach (RectTransform rectTransform in rectTransforms)
            {
                if (rectTransform == null)
                {
                    continue;
                }

                if (rectTransform.name.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return rectTransform;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            foreach (Transform child in parent)
            {
                if (child.name == childName)
                {
                    return child;
                }

                Transform result = FindChildRecursive(child, childName);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static void SetButtonText(GameObject buttonObject, string text)
        {
            TMP_Text label = buttonObject.GetComponentInChildren<TMP_Text>(true);

            if (label != null)
            {
                label.text = text;
                label.alignment = TextAlignmentOptions.Center;
                return;
            }

            Text legacyLabel = buttonObject.GetComponentInChildren<Text>(true);

            if (legacyLabel != null)
            {
                legacyLabel.text = text;
                legacyLabel.alignment = TextAnchor.MiddleCenter;
            }
        }

        private static void StyleActionButton(GameObject buttonObject)
        {
            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();

            if (rectTransform == null)
            {
                return;
            }

            rectTransform.sizeDelta = new Vector2(ButtonSize, ButtonSize);

            Image image = buttonObject.GetComponent<Image>();

            if (image != null)
            {
                image.type = Image.Type.Sliced;
            }
        }

        private static void PositionActionButton(GameObject buttonObject, int index)
        {
            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            RectTransform playerRectTransform = _inventoryGui.m_player.GetComponent<RectTransform>();

            if (rectTransform == null || playerRectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);

            float x = playerRectTransform.rect.width + 8f + (index * (ButtonSize + ButtonSpacing));
            float y = -playerRectTransform.rect.height + ButtonSize;

            rectTransform.anchoredPosition = new Vector2(x, y);
        }

        private static void OnSortClicked()
        {
            InventorySortService.SortPlayerInventory();

            if (_inventoryGui == null)
            {
                return;
            }

            Container container = CurrentContainerField?.GetValue(_inventoryGui) as Container;

            if (container != null)
            {
                InventorySortService.SortContainer(container);
            }
        }

        private static void OnQuickStackClicked()
        {
            Player player = Player.m_localPlayer;

            if (player == null)
            {
                return;
            }

            QuickStackService.QuickStack(player);
        }

        private static void OnTrashClicked()
        {
            Player player = Player.m_localPlayer;

            if (player == null || _inventoryGui == null)
            {
                return;
            }

            ItemDrop.ItemData dragItem = DragItemField?.GetValue(_inventoryGui) as ItemDrop.ItemData;

            if (dragItem != null)
            {
                TrashService.DeleteDraggedItem(_inventoryGui, player);
                return;
            }

            TrashService.DeleteMarkedItems(player);
        }
    }
}