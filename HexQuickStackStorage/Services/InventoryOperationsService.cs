using System.Reflection;

namespace HexQuickStackStorage.Services
{
    internal static class InventoryOperationsService
    {
        private static readonly MethodInfo ChangedMethod = typeof(Inventory).GetMethod("Changed", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo FindFreeStackItemMethod = typeof(Inventory).GetMethod("FindFreeStackItem", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo FindEmptySlotMethod = typeof(Inventory).GetMethod("FindEmptySlot", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo TopFirstMethod = typeof(Inventory).GetMethod("TopFirst", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly object[] ChangedArguments = { false, false };

        internal static void NotifyChanged(Inventory inventory)
        {
            if (inventory == null)
            {
                return;
            }

            ChangedMethod?.Invoke(inventory, ChangedArguments);
        }

        internal static ItemDrop.ItemData FindFreeStackItem(Inventory inventory, ItemDrop.ItemData item)
        {
            if (inventory == null || item == null || FindFreeStackItemMethod == null)
            {
                return null;
            }

            return FindFreeStackItemMethod.Invoke(inventory, new object[]
            {
                item.m_shared.m_name,
                item.m_quality,
                item.m_worldLevel
            }) as ItemDrop.ItemData;
        }

        internal static Vector2i FindEmptySlot(Inventory inventory, ItemDrop.ItemData item)
        {
            if (inventory == null || item == null || FindEmptySlotMethod == null || TopFirstMethod == null)
            {
                return new Vector2i(-1, -1);
            }

            bool topFirst = (bool)TopFirstMethod.Invoke(inventory, new object[] { item });

            return (Vector2i)FindEmptySlotMethod.Invoke(inventory, new object[] { topFirst });
        }
    }
}