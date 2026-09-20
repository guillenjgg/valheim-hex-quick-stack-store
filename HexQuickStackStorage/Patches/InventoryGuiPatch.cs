using HarmonyLib;

namespace HexQuickStackStorage.Patches
{
    [HarmonyPatch]
    internal static class InventoryGuiPatch
    {
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
        [HarmonyPostfix]
        private static void AwakePostfix(InventoryGui __instance)
        {
            InventoryUiController.Initialize(__instance);
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnStackAll))]
        [HarmonyPrefix]
        private static bool OnStackAllPrefix()
        {
            Player player = Player.m_localPlayer;

            if (player == null)
            {
                return true;
            }

            Container container = InventoryUiController.GetCurrentContainer();

            if (container == null)
            {
                return true;
            }

            QuickStackService.QuickStack(player, container);

            return false;
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
        [HarmonyPostfix]
        private static void ShowPostfix(Container container)
        {
            InventoryUiController.SetContainerSortButtonVisible(container != null);
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.CloseContainer))]
        [HarmonyPostfix]
        private static void CloseContainerPostfix()
        {
            InventoryUiController.SetContainerSortButtonVisible(false);
        }
    }
}