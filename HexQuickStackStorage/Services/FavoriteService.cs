using System;
using System.Collections.Generic;
using System.IO;

namespace HexQuickStackStorage
{
    internal static class FavoriteService
    {
        private const string FavoriteFileName = "HexQuickStackStorage.favorites.txt";

        private static readonly HashSet<string> FavoriteItemNames = new HashSet<string>(StringComparer.Ordinal);

        private static string FavoriteFilePath => Path.Combine(BepInEx.Paths.ConfigPath, FavoriteFileName);

        internal static void Initialize()
        {
            LoadFavoriteItems();
        }

        internal static bool IsFavorite(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null)
            {
                return false;
            }

            return FavoriteItemNames.Contains(item.m_shared.m_name);
        }

        internal static void ToggleFavorite(Player player, ItemDrop.ItemData item)
        {
            if (player == null || item == null || item.m_shared == null)
            {
                return;
            }

            if (!TrashService.IsVanillaInventoryItem(player, item))
            {
                return;
            }

            string itemName = item.m_shared.m_name;

            if (!FavoriteItemNames.Add(itemName))
            {
                FavoriteItemNames.Remove(itemName);
            }
            else
            {
                TrashService.Unmark(item);
            }

            SaveFavoriteItems();
        }

        internal static void Unfavorite(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null)
            {
                return;
            }

            if (FavoriteItemNames.Remove(item.m_shared.m_name))
            {
                SaveFavoriteItems();
            }
        }

        private static void LoadFavoriteItems()
        {
            FavoriteItemNames.Clear();

            if (!File.Exists(FavoriteFilePath))
            {
                return;
            }

            foreach (string line in File.ReadAllLines(FavoriteFilePath))
            {
                string itemName = line.Trim();

                if (!string.IsNullOrEmpty(itemName))
                {
                    FavoriteItemNames.Add(itemName);
                }
            }
        }

        private static void SaveFavoriteItems()
        {
            Directory.CreateDirectory(BepInEx.Paths.ConfigPath);

            List<string> itemNames = new List<string>(FavoriteItemNames);
            itemNames.Sort(StringComparer.Ordinal);

            File.WriteAllLines(FavoriteFilePath, itemNames);
        }
    }
}