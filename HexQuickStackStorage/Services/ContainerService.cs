using System.Collections.Generic;
using UnityEngine;

namespace HexQuickStackStorage
{
    internal static class ContainerService
    {
        private const float SearchRadius = 20f;

        internal static List<Container> GetNearbyContainers(Player player)
        {
            List<Container> containers = new List<Container>();

            if (player == null)
            {
                return containers;
            }

            long playerId = Game.instance.GetPlayerProfile().GetPlayerID();
            Collider[] colliders = Physics.OverlapSphere(player.transform.position, SearchRadius);
            HashSet<Container> foundContainers = new HashSet<Container>();

            foreach (Collider collider in colliders)
            {
                Container container = collider.GetComponentInParent<Container>();

                if (container == null)
                {
                    continue;
                }

                if (!foundContainers.Add(container))
                {
                    continue;
                }

                if (!IsPlayerOwnedContainer(container, playerId))
                {
                    continue;
                }

                containers.Add(container);
            }

            return containers;
        }

        private static bool IsPlayerOwnedContainer(Container container, long playerId)
        {
            Piece piece = container.GetComponent<Piece>();

            if (piece == null)
            {
                return false;
            }

            return piece.GetCreator() == playerId;
        }
    }
}