using HarmonyLib;

namespace HexQuickStackStorage.Patches
{
    [HarmonyPatch(typeof(Container))]
    internal static class ContainerPatch
    {
        [HarmonyPatch(nameof(Container.RPC_OpenResponse))]
        [HarmonyPostfix]
        private static void RPC_OpenResponsePostfix(Container __instance, bool granted)
        {
            if (!granted)
            {
                return;
            }

            if (!Plugin.EnableChestAutoSorting)
            {
                return;
            }

            InventorySortService.SortContainer(__instance);
        }

        [HarmonyPatch(nameof(Container.RPC_StackResponse))]
        [HarmonyPrefix]
        private static bool RPC_StackResponsePrefix(Container __instance, bool granted)
        {
            if (QuickStackService.CompleteNearbyStackRequest(__instance, granted))
            {
                return false;
            }

            if (!granted)
            {
                return true;
            }

            // Ignore untracked or delayed replies unless this chest is currently open.
            if (UI.InventoryUiController.GetCurrentContainer() != __instance)
            {
                return false;
            }

            Player player = Player.m_localPlayer;

            if (player == null)
            {
                return false;
            }

            QuickStackService.QuickStack(player, __instance);

            return false;
        }
    }
}
