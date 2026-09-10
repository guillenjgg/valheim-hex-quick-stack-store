using HarmonyLib;

namespace HexQuickStackStorage.Patches
{
    [HarmonyPatch(typeof(Container), nameof(Container.RPC_OpenResponse))]
    internal static class ContainerPatch
    {
        private static void Postfix(Container __instance, bool granted)
        {
            if (!granted)
            {
                return;
            }

            Player player = Player.m_localPlayer;

            if (player == null)
            {
                return;
            }

            long playerId = Game.instance.GetPlayerProfile().GetPlayerID();

            if (!ContainerService.IsPlayerOwnedContainer(__instance, playerId))
            {
                return;
            }

            InventorySortService.SortContainer(__instance);
        }
    }
}