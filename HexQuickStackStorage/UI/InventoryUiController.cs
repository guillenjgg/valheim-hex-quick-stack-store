using HarmonyLib;
using System.Collections.Generic;
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

        private const float ButtonSize = 36f;
        private const float ButtonSpacing = 4f;

        private static readonly FieldInfo CurrentContainerField = AccessTools.Field(typeof(InventoryGui), "m_currentContainer");

        private static InventoryGui _inventoryGui;
        private static Button _sortButton;
        private static Button _quickStackButton;

        internal static void Initialize(InventoryGui inventoryGui)
        {
            _inventoryGui = inventoryGui;

            LogPlayerInventoryDimensions();
            LogNativeButtonDimensions();
            CreateButtons();
        }

        private static void CreateButtons()
        {
            if (_inventoryGui == null)
            {
                return;
            }

            Button nativeButton = _inventoryGui.m_takeAllButton;

            if (nativeButton == null)
            {
                Plugin.Log.LogWarning("Could not find Valheim Take All button.");
                return;
            }

            _quickStackButton = CreateButton(
                nativeButton,
                QuickStackButtonName,
                "Q",
                1,
                OnQuickStackClicked
            );

            _sortButton = CreateButton(
                nativeButton,
                SortButtonName,
                "S",
                0,
                OnSortClicked
            );

            Plugin.Log.LogInfo("Inventory action buttons created.");
        }

        private static Button CreateButton(
            Button template,
            string buttonName,
            string text,
            int index,
            UnityEngine.Events.UnityAction onClick
        )
        {
            Transform existing = _inventoryGui.m_player.transform.Find(buttonName);

            if (existing != null)
            {
                return existing.GetComponent<Button>();
            }

            GameObject buttonObject = Object.Instantiate(
                template.gameObject,
                _inventoryGui.m_player.transform
            );

            buttonObject.name = buttonName;

            Button button = buttonObject.GetComponent<Button>();

            if (button == null)
            {
                Plugin.Log.LogWarning(
                    $"{buttonName} does not contain a Button component."
                );

                Object.Destroy(buttonObject);
                return null;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);

            SetButtonText(buttonObject, text);
            StyleButton(buttonObject);
            PositionButton(buttonObject, index);

            buttonObject.SetActive(true);

            return button;
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

        private static void StyleButton(GameObject buttonObject)
        {
            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();

            if (rectTransform == null)
            {
                return;
            }

            rectTransform.sizeDelta = new Vector2(
                ButtonSize,
                ButtonSize
            );

            Image image = buttonObject.GetComponent<Image>();

            if (image != null)
            {
                image.type = Image.Type.Sliced;
            }
        }

        private static void PositionButton(GameObject buttonObject, int index)
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

            Plugin.Log.LogInfo($"{buttonObject.name} positioned at x={x:0.0}, y={y:0.0}");
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
                Plugin.Log.LogWarning("No local player found.");
                return;
            }

            QuickStackService.QuickStack(player);
        }

        private static void LogPlayerInventoryDimensions()
        {
            if (_inventoryGui?.m_player == null)
            {
                Plugin.Log.LogWarning("Player inventory UI was not found.");
                return;
            }

            RectTransform rectTransform = _inventoryGui.m_player.GetComponent<RectTransform>();

            if (rectTransform == null)
            {
                Plugin.Log.LogWarning("Player inventory does not have a RectTransform.");
                return;
            }

            Rect rect = rectTransform.rect;

            Plugin.Log.LogInfo($"Player Inventory UI:");
            Plugin.Log.LogInfo($"  Size: {rect.width:0.0} x {rect.height:0.0}");
            Plugin.Log.LogInfo($"  Rect: x={rect.x:0.0}, y={rect.y:0.0}, w={rect.width:0.0}, h={rect.height:0.0}");
            Plugin.Log.LogInfo($"  Anchored Position: x={rectTransform.anchoredPosition.x:0.0}, y={rectTransform.anchoredPosition.y:0.0}");
            Plugin.Log.LogInfo($"  Pivot: x={rectTransform.pivot.x:0.00}, y={rectTransform.pivot.y:0.00}");
            Plugin.Log.LogInfo($"  Anchor Min: x={rectTransform.anchorMin.x:0.00}, y={rectTransform.anchorMin.y:0.00}");
            Plugin.Log.LogInfo($"  Anchor Max: x={rectTransform.anchorMax.x:0.00}, y={rectTransform.anchorMax.y:0.00}");
        }

        private static void LogNativeButtonDimensions()
        {
            if (_inventoryGui?.m_takeAllButton == null)
            {
                Plugin.Log.LogWarning("Valheim Take All button was not found.");
                return;
            }

            RectTransform rectTransform = _inventoryGui.m_takeAllButton.GetComponent<RectTransform>();

            if (rectTransform == null)
            {
                Plugin.Log.LogWarning("Valheim Take All button does not have a RectTransform.");
                return;
            }

            Rect rect = rectTransform.rect;

            Plugin.Log.LogInfo("Vanilla Take All Button:");
            Plugin.Log.LogInfo($"  Name: {_inventoryGui.m_takeAllButton.name}");
            Plugin.Log.LogInfo($"  Size: {rect.width:0.0} x {rect.height:0.0}");
            Plugin.Log.LogInfo($"  Size Delta: x={rectTransform.sizeDelta.x:0.0}, y={rectTransform.sizeDelta.y:0.0}");
            Plugin.Log.LogInfo($"  Anchored Position: x={rectTransform.anchoredPosition.x:0.0}, y={rectTransform.anchoredPosition.y:0.0}");
            Plugin.Log.LogInfo($"  Pivot: x={rectTransform.pivot.x:0.00}, y={rectTransform.pivot.y:0.00}");
            Plugin.Log.LogInfo($"  Anchor Min: x={rectTransform.anchorMin.x:0.00}, y={rectTransform.anchorMin.y:0.00}");
            Plugin.Log.LogInfo($"  Anchor Max: x={rectTransform.anchorMax.x:0.00}, y={rectTransform.anchorMax.y:0.00}");
        }
    }
}