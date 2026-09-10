using System.Collections.Generic;
using UnityEngine;

namespace HexQuickStackStorage
{
    internal static class ContainerService
    {
        internal static List<Container> GetNearbyContainers(Player player)
        {
            List<Container> containers = new List<Container>();

            if (player == null)
            {
                return containers;
            }

            long playerId = Game.instance.GetPlayerProfile().GetPlayerID();
            Collider[] colliders = Physics.OverlapSphere(player.transform.position, Plugin.SearchRadius);
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

        internal static bool IsPlayerOwnedContainer(Container container, long playerId)
        {
            if (container == null)
            {
                return false;
            }

            Piece piece = container.GetComponent<Piece>();

            if (piece == null)
            {
                return false;
            }

            return piece.GetCreator() == playerId;
        }
    }
}