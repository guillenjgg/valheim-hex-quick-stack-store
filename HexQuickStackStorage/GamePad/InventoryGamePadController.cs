using TMPro;
using UnityEngine;

namespace HexQuickStackStorage.GamePad
{
    internal static class InventoryGamePadController
    {
        private const string SortButtonName = Plugin.PluginGuid + ".SortButton";
        private const string QuickStackButtonName = Plugin.PluginGuid + ".QuickStackButton";
        private const string QuickStackGamePadBinding = "JoyRStick";

        private static UIGamePad _takeAllGamePad;
        private static UIGamePad _stackAllGamePad;
        private static UIGamePad _sortGamePad;
        private static UIGamePad _quickStackGamePad;

        internal static void Initialize(InventoryGui inventoryGui)
        {
            if (inventoryGui == null || inventoryGui.m_player == null)
            {
                return;
            }

            if (inventoryGui.m_takeAllButton != null)
            {
                _takeAllGamePad = inventoryGui.m_takeAllButton.GetComponent<UIGamePad>();
            }

            if (inventoryGui.m_stackAllButton != null)
            {
                _stackAllGamePad = inventoryGui.m_stackAllButton.GetComponent<UIGamePad>();
            }

            Transform sortButton = inventoryGui.m_player.transform.Find(SortButtonName);

            if (sortButton != null)
            {
                _sortGamePad = sortButton.GetComponent<UIGamePad>();
            }

            Transform quickStackButton = inventoryGui.m_player.transform.Find(QuickStackButtonName);

            if (quickStackButton != null)
            {
                _quickStackGamePad = quickStackButton.GetComponent<UIGamePad>();

                if (_quickStackGamePad != null)
                {
                    _quickStackGamePad.m_zinputKey = QuickStackGamePadBinding;

                    UpdateQuickStackHint();
                }
            }

            SetContainerOpen(false);
        }

        internal static void SetContainerOpen(bool containerOpen)
        {
            if (_takeAllGamePad != null)
            {
                _takeAllGamePad.enabled = containerOpen;
            }

            if (_stackAllGamePad != null)
            {
                _stackAllGamePad.enabled = containerOpen;
            }

            if (_sortGamePad != null)
            {
                _sortGamePad.enabled = !containerOpen;
            }

            if (_quickStackGamePad != null)
            {
                _quickStackGamePad.enabled = !containerOpen;
            }
        }

        private static void UpdateQuickStackHint()
        {
            if (_quickStackGamePad == null || _quickStackGamePad.m_hint == null)
            {
                return;
            }

            TMP_Text hintText = _quickStackGamePad.m_hint.GetComponentInChildren<TMP_Text>(true);

            if (hintText == null)
            {
                Plugin.Log.LogWarning("Could not find Quick Stack gamepad hint text.");
                return;
            }

            hintText.text = Localization.instance.Localize($"$KEY_{QuickStackGamePadBinding}");
        }
    }
}