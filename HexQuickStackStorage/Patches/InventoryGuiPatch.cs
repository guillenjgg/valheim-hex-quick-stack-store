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

        [HarmonyPatch(typeof(InventoryGui), "OnStackAll")]
        [HarmonyPrefix]
        private static bool OnStackAllPrefix()
        {
            Player player = Player.m_localPlayer;

            if (player == null)
            {
                return false;
            }

            Container container = InventoryUiController.GetCurrentContainer();

            if (container == null)
            {
                return false;
            }

            QuickStackService.QuickStack(player, container);

            return false;
        }
    }
}