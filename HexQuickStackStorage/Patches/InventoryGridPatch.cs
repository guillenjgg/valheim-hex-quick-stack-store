using HarmonyLib;
using UnityEngine;

namespace HexQuickStackStorage.Patches
{
    [HarmonyPatch]
    internal static class InventoryGridPatch
    {
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnRightClickItem))]
        [HarmonyPrefix]
        private static bool OnRightClickItemPrefix(InventoryGrid grid, ItemDrop.ItemData item)
        {
            Player player = Player.m_localPlayer;

            if (player == null || grid == null || item == null)
            {
                return true;
            }

            if (grid.GetInventory() != player.GetInventory())
            {
                return true;
            }

            if (!TrashService.IsVanillaInventoryItem(player, item))
            {
                return true;
            }

            if (ZInput.GetKey(Plugin.TrashModifierKey, true))
            {
                TrashService.ToggleMarked(player, item);
                UI.TrashBorderRenderer.Refresh(grid);

                return false;
            }

            if (ZInput.GetKey(Plugin.FavoriteModifierKey, true))
            {
                // FavoriteService.ToggleFavorite(player, item);
                // UI.FavoriteBorderRenderer.Refresh(grid);

                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
        [HarmonyPostfix]
        private static void UpdateGuiPostfix(InventoryGrid __instance)
        {
            UI.TrashBorderRenderer.Refresh(__instance);

            // UI.FavoriteBorderRenderer.Refresh(__instance);
        }
    }
}