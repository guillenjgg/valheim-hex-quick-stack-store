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

            if (!Plugin.EnableChestAutoSorting)
            {
                return;
            }

            Player player = Player.m_localPlayer;

            if (player == null || Game.instance == null)
            {
                return;
            }

            PlayerProfile playerProfile = Game.instance.GetPlayerProfile();

            if (playerProfile == null)
            {
                return;
            }

            long playerId = playerProfile.GetPlayerID();

            if (!ContainerService.WasCreatedByPlayer(__instance, playerId))
            {
                return;
            }

            InventorySortService.SortContainer(__instance);
        }
    }
}