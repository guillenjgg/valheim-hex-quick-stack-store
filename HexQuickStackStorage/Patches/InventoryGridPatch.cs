using HarmonyLib;
using UnityEngine;

namespace HexQuickStackStorage.Patches
{
    [HarmonyPatch]
    internal static class InventoryGridPatch
    {
        private const string TrashBorderName = "HexTrashBorder";
        private const string FavoriteBorderName = "HexFavoriteBorder";

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnRightClickItem))]
        [HarmonyPrefix]
        private static bool OnRightClickItemPrefix(InventoryGrid grid, ItemDrop.ItemData item)
        {
            Player player = Player.m_localPlayer;

            if (player == null)
            {
                return true;
            }

            if (grid == null)
            {
                return true;
            }

            if (item == null)
            {
                return true;
            }

            if (grid.GetInventory() != player.GetInventory())
            {
                return true;
            }

            if (!TrashService.IsPlayerStorageItem(player, item))
            {
                return true;
            }

            if (Plugin.EnableTrashFeature && ZInput.GetKey(Plugin.TrashModifierKey, true))
            {
                ItemStateService.ToggleTrash(player, item);
                RefreshItemStateBorders(grid);

                return false;
            }

            if (ZInput.GetKey(Plugin.FavoriteModifierKey, true))
            {
                ItemStateService.ToggleFavorite(player, item);
                RefreshItemStateBorders(grid);

                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
        [HarmonyPostfix]
        private static void UpdateGuiPostfix(InventoryGrid __instance)
        {
            RefreshItemStateBorders(__instance);
        }

        private static void RefreshItemStateBorders(InventoryGrid grid)
        {
            if (Plugin.EnableTrashFeature)
            {
                UI.InventoryBorderRenderer.Refresh(grid, TrashBorderName, Color.red, TrashService.IsMarked);
            }
            else
            {
                UI.InventoryBorderRenderer.Refresh(grid, TrashBorderName, Color.red, item => false);
            }

            UI.InventoryBorderRenderer.Refresh(grid, FavoriteBorderName, new Color(1f, 0.75f, 0f), FavoriteService.IsFavorite);
        }
    }
}
