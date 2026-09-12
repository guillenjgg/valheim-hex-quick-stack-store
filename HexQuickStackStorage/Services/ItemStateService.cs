namespace HexQuickStackStorage
{
    internal static class ItemStateService
    {
        internal static void ToggleFavorite(Player player, ItemDrop.ItemData item)
        {
            if (player == null || item == null || item.m_shared == null)
            {
                return;
            }

            if (FavoriteService.IsFavorite(item))
            {
                FavoriteService.Unfavorite(item);
                return;
            }

            TrashService.Unmark(item);
            FavoriteService.Favorite(item);
        }

        internal static void ToggleTrash(Player player, ItemDrop.ItemData item)
        {
            if (player == null || item == null || item.m_shared == null)
            {
                return;
            }

            if (!TrashService.IsVanillaInventoryItem(player, item))
            {
                return;
            }

            if (IsHotbarItem(item))
            {
                player.Message(
                    MessageHud.MessageType.Center,
                    "Hotbar items cannot be marked as trash.",
                    0,
                    null
                );

                return;
            }

            if (item.m_equipped)
            {
                player.Message(
                    MessageHud.MessageType.Center,
                    "Equipped items cannot be marked as trash.",
                    0,
                    null
                );

                return;
            }

            if (TrashService.IsMarked(item))
            {
                TrashService.Unmark(item);
                return;
            }

            FavoriteService.Unfavorite(item);
            TrashService.Mark(item);
        }

        internal static bool CanDelete(ItemDrop.ItemData item)
        {
            if (item == null)
            {
                return false;
            }

            if (item.m_equipped)
            {
                return false;
            }

            if (IsHotbarItem(item))
            {
                return false;
            }

            if (FavoriteService.IsFavorite(item))
            {
                return false;
            }

            return true;
        }

        internal static bool IsHotbarItem(ItemDrop.ItemData item)
        {
            return item != null && item.m_gridPos.y == 0;
        }
    }
}