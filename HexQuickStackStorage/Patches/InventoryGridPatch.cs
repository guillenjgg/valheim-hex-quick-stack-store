using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace HexQuickStackStorage.Patches
{
    [HarmonyPatch(typeof(InventoryGrid))]
    internal static class InventoryGridPatch
    {
        private static readonly MethodInfo GetButtonPosMethod = AccessTools.Method(typeof(InventoryGrid), "GetButtonPos", new[] { typeof(GameObject) });

        [HarmonyPatch(nameof(InventoryGrid.OnRightDown))]
        [HarmonyPrefix]
        private static bool OnRightDownPrefix(InventoryGrid __instance, UIInputHandler element)
        {
            Player player = Player.m_localPlayer;

            if (player == null || __instance.GetInventory() != player.GetInventory())
            {
                return true;
            }

            if (!ZInput.GetKey(KeyCode.LeftShift, true) && !ZInput.GetKey(KeyCode.RightShift, true))
            {
                return true;
            }

            if (GetButtonPosMethod == null)
            {
                return true;
            }

            Vector2i position = (Vector2i)GetButtonPosMethod.Invoke(__instance, new object[] { element.gameObject });
            ItemDrop.ItemData item = __instance.GetInventory().GetItemAt(position.x, position.y);

            if (item == null)
            {
                return true;
            }

            if (!TrashService.IsVanillaInventoryItem(player, item))
            {
                return true;
            }

            TrashService.ToggleMarked(player, item);
            UI.TrashBorderRenderer.Refresh(__instance);

            return false;
        }

        [HarmonyPatch(nameof(InventoryGrid.UpdateGui))]
        [HarmonyPostfix]
        private static void UpdateGuiPostfix(InventoryGrid __instance)
        {
            UI.TrashBorderRenderer.Refresh(__instance);
        }
    }
}