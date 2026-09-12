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
            UI.InventoryBorderRenderer.Refresh(
                grid,
                "HexTrashBorder",
                Color.red,
                TrashService.IsMarked
            );

            UI.InventoryBorderRenderer.Refresh(
                grid,
                "HexFavoriteBorder",
                new Color(1f, 0.75f, 0f),
                FavoriteService.IsFavorite
            );
        }
    }
}