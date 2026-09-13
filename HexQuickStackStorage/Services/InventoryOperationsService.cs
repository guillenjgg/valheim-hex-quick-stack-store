using System.Reflection;

namespace HexQuickStackStorage.Services
{
    internal static class InventoryOperationsService
    {
        private static readonly MethodInfo ChangedMethod =
            typeof(Inventory).GetMethod(
                "Changed",
                BindingFlags.Instance |
                BindingFlags.NonPublic
            );

        private static readonly object[] ChangedArguments =
        {
            false,
            false
        };

        internal static void NotifyChanged(Inventory inventory)
        {
            if (inventory == null)
            {
                return;
            }

            ChangedMethod?.Invoke(
                inventory,
                ChangedArguments
            );
        }
    }
}