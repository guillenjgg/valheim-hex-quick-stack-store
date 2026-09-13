using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace HexQuickStackStorage
{
    internal static class ContainerService
    {
        private static readonly MethodInfo CheckAccessMethod = typeof(Container).GetMethod("CheckAccess", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        internal static List<Container> GetNearbyContainers(Player player)
        {
            List<Container> containers = new List<Container>();

            if (player == null || Game.instance == null)
            {
                return containers;
            }

            Collider[] colliders = Physics.OverlapSphere(player.transform.position, Plugin.SearchRadius);

            HashSet<Container> foundContainers = new HashSet<Container>();

            foreach (Collider collider in colliders)
            {
                if (collider == null)
                {
                    continue;
                }

                Container container = collider.GetComponentInParent<Container>();

                if (container == null)
                {
                    continue;
                }

                if (!foundContainers.Add(container))
                {
                    continue;
                }

                if (!CanUseContainer(container))
                {
                    continue;
                }

                containers.Add(container);
            }

            return containers;
        }

        internal static bool CanUseContainer(Container container)
        {
            if (container == null || Game.instance == null)
            {
                return false;
            }

            PlayerProfile playerProfile = Game.instance.GetPlayerProfile();

            if (playerProfile == null)
            {
                return false;
            }

            if (container.m_checkGuardStone && !PrivateArea.CheckAccess(container.transform.position, 0f, true, false))
            {
                return false;
            }

            long playerId = playerProfile.GetPlayerID();

            switch (Plugin.ContainerAccessMode)
            {
                case ContainerAccessModeEnum.CharacterOwned:
                    return WasCreatedByPlayer(container, playerId);

                case ContainerAccessModeEnum.Accessible:
                    return CheckAccess(container, playerId);

                default:
                    return false;
            }
        }

        internal static bool WasCreatedByPlayer(Container container, long playerId)
        {
            if (container == null)
            {
                return false;
            }

            Piece piece = container.GetComponentInParent<Piece>();

            if (piece == null)
            {
                return false;
            }

            return piece.GetCreator() == playerId;
        }

        private static bool CheckAccess(Container container, long playerId)
        {
            if (container == null || CheckAccessMethod == null)
            {
                return false;
            }

            return (bool)CheckAccessMethod.Invoke(container, new object[] { playerId });
        }
    }
}