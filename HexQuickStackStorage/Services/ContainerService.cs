using System.Collections.Generic;
using UnityEngine;

namespace HexQuickStackStorage
{
    internal static class ContainerService
    {
        internal static List<Container> GetNearbyContainers(Player player)
        {
            List<Container> containers = new List<Container>();

            if (player == null || Game.instance == null)
            {
                return containers;
            }

            PlayerProfile playerProfile = Game.instance.GetPlayerProfile();

            if (playerProfile == null)
            {
                return containers;
            }

            long playerId = playerProfile.GetPlayerID();

            Collider[] colliders = Physics.OverlapSphere(
                player.transform.position,
                Plugin.SearchRadius
            );

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

                if (!WasCreatedByPlayer(container, playerId))
                {
                    continue;
                }

                containers.Add(container);
            }

            return containers;
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
    }
}