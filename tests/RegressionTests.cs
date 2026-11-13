// Standalone game doubles exercise the production services without launching Unity.
using System;
using System.Collections.Generic;
using HexQuickStackStorage;
using HexQuickStackStorage.Services;

public static class RegressionTests
{
    private static int _checks;

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }

        _checks++;
    }

    private static Player Reset(int height = 6)
    {
        QuickStackService.ClearPendingRequests();
        ContainerService.Nearby.Clear();

        ContainerService.Allowed = true;

        FavoriteService.Favorites.Clear();

        UnityEngine.Time.realtimeSinceStartup = 0;
        Player player = Player.m_localPlayer = new Player
        {
            Inventory = new Inventory(2, height)
        };
        InventoryGui.instance = new InventoryGui
        {
            m_playerGrid = new InventoryGrid(player.Inventory, 2, height)
        };

        return player;
    }

    private static void SetNormalGrid()
    {
        InventoryGui.instance.m_playerGrid.Rows = 6;
    }

    private static int StorageHeight(Player player)
    {
        PlayerInventoryService.TryGetStorageSize(player, out int width, out int height);

        return height;
    }

    private static ItemDrop.ItemData Item(string name, int y, int amount = 1, int x = 0)
    {
        return new ItemDrop.ItemData
        {
            m_shared = new ItemDrop.SharedData
            {
                m_name = name
            },
            m_gridPos = new Vector2i(x, y),
            m_stack = amount
        };
    }

    private static Container Chest(Player player)
    {
        var chest = new Container();

        chest.Inventory.Items.Add(Item("Wood", 0, 5));
        player.Inventory.Items.Add(Item("Wood", 5, 3));
        ContainerService.Nearby.Add(chest);

        return chest;
    }

    private static bool Reply(Container chest, bool granted)
    {
        var method = typeof(HexQuickStackStorage.Patches.ContainerPatch).GetMethod("RPC_StackResponsePrefix", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        return (bool)method.Invoke(null, new object[] { chest, granted });
    }

    public static void Run()
    {
        Player p = Reset();

        Check(StorageHeight(p) == 6, "Ordinary expansion must use live height, not saved four rows");

        p.Inventory.Height = 3;
        InventoryGui.instance.m_playerGrid.Rows = 3;

        Check(StorageHeight(p) == 3, "Shrinking must use live height");

        p = Reset(9);

        SetNormalGrid();
        Check(StorageHeight(p) == 6, "Exclude rows outside the player grid");
        Check(PlayerInventoryService.IsStorageItem(p, Item("Wood", 5)), "Include added storage row");
        Check(!PlayerInventoryService.IsStorageItem(p, Item("Wood", 6)), "Protect special row");
        Check(!PlayerInventoryService.IsStorageItem(p, Item("Wood", 2, 1, 2)), "Reject invalid columns");

        InventoryGui.instance.m_playerGrid.Rows = 7;

        Check(StorageHeight(p) == 7, "Read live player grid size");

        InventoryGrid grid = InventoryGui.instance.m_playerGrid;
        InventoryGui.instance = null;

        Check(StorageHeight(p) == 0, "Unavailable UI must not expose hidden slots");

        InventoryGui.instance = new InventoryGui
        {
            m_playerGrid = grid
        };
        grid.Columns = 1;

        Check(!PlayerInventoryService.IsStorageItem(p, Item("Wood", 2, 1, 1)), "Use displayed width, not backing inventory width");

        grid.Columns = 2;
        grid.m_gridRoot.rect.width = 134f;
        grid.m_gridRoot.rect.height = 414f;

        Check(StorageHeight(p) == 6, "Count final slots without trailing cell spacing");

        p = Reset(9);

        SetNormalGrid();

        var hotbar = Item("Hotbar", 0);
        var favorite = Item("Favorite", 1);
        var equipped = Item("Equipped", 2);
        equipped.m_equipped = true;
        var extra = Item("Wood", 5, 3);
        var special = Item("Special", 6);

        p.Inventory.Items.AddRange(new[] { hotbar, favorite, equipped, extra, special });
        FavoriteService.Favorites.Add(favorite);
        InventorySortService.SortPlayerInventory();
        Check(extra.m_gridPos.y == 1 && extra.m_gridPos.x == 1, "Sort items from added storage rows");
        Check(special.m_gridPos.y == 6 && hotbar.m_gridPos.y == 0 && equipped.m_gridPos.y == 2 && favorite.m_gridPos.y == 1, "Sort must preserve special, equipped, hotbar, and favorite items");

        p = Reset();
        Container c = Chest(p);

        QuickStackService.QuickStack(p);
        Check(c.Requests == 1 && p.Inventory.Items.Count == 1 && c.Inventory.Items[0].m_stack == 5, "Request access without transferring items");
        QuickStackService.QuickStack(p);
        Check(c.Requests == 1, "Do not submit duplicate active requests");
        Check(!Reply(c, true) && p.Inventory.Items.Count == 0 && c.Inventory.Items[0].m_stack == 8, "Transfer only after granted access");
        Reply(c, true);
        Check(c.Inventory.Items[0].m_stack == 8, "Ignore duplicate/untracked grant");

        foreach (string rejection in new[]
        {
            "denied",
            "owner",
            "timeout",
            "favorite",
            "equipped",
            "removed",
            "inventory",
            "access",
            "range",
            "busy",
            "character"
        }

        )
        {
            p = Reset();
            c = Chest(p);

            QuickStackService.QuickStack(p);

            switch (rejection)
            {
                case "owner":
                    c.m_nview.Owner = false;

                    break;
                case "timeout":
                    UnityEngine.Time.realtimeSinceStartup = 11;

                    break;
                case "favorite":
                    FavoriteService.Favorites.Add(p.Inventory.Items[0]);

                    break;
                case "equipped":
                    p.Inventory.Items[0].m_equipped = true;

                    break;
                case "removed":
                    p.Inventory.Items.Clear();

                    break;
                case "inventory":
                    p.Inventory = new Inventory(2, 6);

                    break;
                case "access":
                    ContainerService.Allowed = false;

                    break;
                case "range":
                    c.transform.position = new UnityEngine.Vector3
                    {
                        x = 100
                    };

                    break;
                case "busy":
                    c.Busy = true;

                    break;
                case "character":
                    Player.m_localPlayer = new Player();

                    break;
            }

            Check(!Reply(c, rejection != "denied") && c.Inventory.Items[0].m_stack == 5, "Reject transfer after " + rejection);
        }

        p = Reset(9);

        SetNormalGrid();

        c = Chest(p);
        var protectedItem = Item("Wood", 6, 7);

        p.Inventory.Items.Add(protectedItem);
        QuickStackService.QuickStack(p);
        Reply(c, true);
        Check(c.Inventory.Items[0].m_stack == 8 && protectedItem.m_stack == 7 && p.Inventory.Items.Contains(protectedItem), "Quick Stack added storage rows while protecting special slots");

        p = Reset();
        c = Chest(p);
        c.Inventory.Items[0].m_stack = 45;
        p.Inventory.Items[0].m_stack = 53;
        var secondChest = new Container();

        secondChest.Inventory.Items.Add(Item("Wood", 0, 49));
        ContainerService.Nearby.Add(secondChest);
        QuickStackService.QuickStack(p);
        Check(c.Requests == 1 && secondChest.Requests == 1, "Request both nearby chests");
        Reply(secondChest, true);
        Reply(c, true);

        int total = 0;

        foreach (var inventory in new[]
        {
            p.Inventory,
            c.Inventory,
            secondChest.Inventory
        }

        )
        {
            foreach (var item in inventory.Items)
            {
                total += item.m_stack;
            }
        }

        Check(total == 147 && p.Inventory.Items.Count == 0, "Out-of-order grants conserve items across partial stacks and newly created stacks");

        p = Reset();
        c = Chest(p);
        c.Busy = true;

        QuickStackService.QuickStack(p);
        Check(c.Requests == 0, "Skip chests already in use");
        CheckLiveInventoryChanges();
        System.Console.WriteLine("Passed " + _checks + " regression checks.");
    }

    private static void CheckLiveInventoryChanges()
    {
        foreach (string change in new[]
        {
            "grow",
            "shrink",
            "narrow",
            "special-grow",
            "special-shrink"
        }

        )
        {
            Player player = Reset(9);

            SetNormalGrid();

            Container chest = Chest(player);
            ItemDrop.ItemData item = player.Inventory.Items[0];

            QuickStackService.QuickStack(player);

            switch (change)
            {
                case "grow":
                    player.Inventory.Height = 12;
                    InventoryGui.instance.m_playerGrid.Rows = 12;
                    item.m_gridPos = new Vector2i(0, 11);

                    break;
                case "shrink":
                    player.Inventory.Height = 4;
                    InventoryGui.instance.m_playerGrid.Rows = 4;

                    break;
                case "narrow":
                    item.m_gridPos = new Vector2i(1, 5);
                    player.Inventory.Width = 1;
                    InventoryGui.instance.m_playerGrid.Columns = 1;

                    break;
                case "special-grow":
                    InventoryGui.instance.m_playerGrid.Rows = 7;
                    item.m_gridPos = new Vector2i(0, 6);

                    break;
                case "special-shrink":
                    InventoryGui.instance.m_playerGrid.Rows = 5;

                    break;
            }

            bool shouldMove = change == "grow" || change == "special-grow";

            Check(PlayerInventoryService.IsStorageItem(player, item) == shouldMove, "Trash/mark eligibility follows " + change);
            Reply(chest, true);
            Check(chest.Inventory.Items[0].m_stack == (shouldMove ? 8 : 5), "Pending Quick Stack uses current dimensions after " + change);
        }

        Player p = Reset(9);

        SetNormalGrid();

        ItemDrop.ItemData excluded = Item("Outside", 5);

        p.Inventory.Items.Add(excluded);
        p.Inventory.Items.Add(Item("Inside", 3));

        InventoryGui.instance.m_playerGrid.Rows = 4;

        InventorySortService.SortPlayerInventory();
        Check(excluded.m_gridPos.y == 5, "Sorting respects a decreased storage boundary");

        InventoryGui.instance.m_playerGrid.Rows = 6;

        InventorySortService.SortPlayerInventory();
        Check(excluded.m_gridPos.y < 4, "Next sort includes newly available storage rows");
    }
}

namespace UnityEngine
{
    public static class Time
    {
        public static float realtimeSinceStartup;
    }

    public static class Mathf
    {
        public static float Max(float a, float b)
        {
            return Math.Max(a, b);
        }

        public static int FloorToInt(float value)
        {
            return (int)Math.Floor(value);
        }

        public static int Clamp(int v, int min, int max)
        {
            return Math.Min(max, Math.Max(min, v));
        }

        public static int Min(int a, int b)
        {
            return Math.Min(a, b);
        }
    }

    public struct Vector3
    {
        public float x;

        public static float Distance(Vector3 a, Vector3 b)
        {
            return Math.Abs(a.x - b.x);
        }
    }

    public class Transform
    {
        public Vector3 position;
    }

    public struct Rect
    {
        public float width, height;
    }

    public class RectTransform
    {
        public Rect rect;
    }

    public class GameObject
    {
        public RectTransform Slot = new RectTransform
        {
            rect = new Rect
            {
                width = 64,
                height = 64
            }
        };

        public T GetComponent<T>()
            where T : class
        {
            return Slot as T;
        }
    }
}

public class InventoryGui
{
    public static InventoryGui instance;

    public InventoryGrid m_playerGrid;
}

public class InventoryGrid
{
    private readonly Inventory _inventory;

    public UnityEngine.RectTransform m_gridRoot = new UnityEngine.RectTransform();
    public UnityEngine.GameObject m_elementPrefab = new UnityEngine.GameObject();
    public float m_elementSpace = 70;

    public int Rows
    {
        set
        {
            m_gridRoot.rect.height = value * m_elementSpace;
        }
    }

    public int Columns
    {
        set
        {
            m_gridRoot.rect.width = value * m_elementSpace;
        }
    }

    public InventoryGrid(Inventory inventory, int columns, int rows)
    {
        _inventory = inventory;
        Columns = columns;
        Rows = rows;
    }

    public Inventory GetInventory()
    {
        return _inventory;
    }
}

public struct Vector2i
{
    public int x, y;

    public Vector2i(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

public class ItemDrop
{
    public class SharedData
    {
        public string m_name;
        public int m_maxStackSize = 50;
        public ItemData.ItemType m_itemType;
    }

    public class ItemData
    {
        public enum ItemType
        {
            Material,
            Trophy
        }

        public SharedData m_shared;
        public Vector2i m_gridPos;
        public int m_stack, m_quality, m_worldLevel;
        public bool m_equipped;

        public ItemData Clone()
        {
            return (ItemData)MemberwiseClone();
        }
    }
}

public class Inventory
{
    public readonly List<ItemDrop.ItemData> Items = new List<ItemDrop.ItemData>();

    public int Width, Height;

    public Inventory(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public int GetWidth()
    {
        return Width;
    }

    public int GetHeight()
    {
        return Height;
    }

    public List<ItemDrop.ItemData> GetAllItems()
    {
        return Items;
    }

    public bool RemoveItem(ItemDrop.ItemData item)
    {
        return Items.Remove(item);
    }

    public bool ContainsItemByName(string name)
    {
        return Items.Exists(i => i.m_shared.m_name == name);
    }
}

public class Player
{
    public const string InventoryRowsKey = "invrows";

    public static Player m_localPlayer;

    public Inventory Inventory = new Inventory(2, 6);
    public UnityEngine.Transform transform = new UnityEngine.Transform();

    public Inventory GetInventory()
    {
        return Inventory;
    }

    public bool TryGetUniqueKeyValue(string key, out string value)
    {
        value = "4";

        return true;
    }

    public bool IsItemEquiped(ItemDrop.ItemData item)
    {
        return item.m_equipped;
    }

    public void Message(MessageHud.MessageType type, string text, int amount, object icon, bool unused)
    {
    }
}

public static class MessageHud
{
    public enum MessageType
    {
        TopLeft
    }
}

public class Container
{
    public ZNetView m_nview = new ZNetView();
    public Wagon m_wagon;
    public UnityEngine.Transform transform = new UnityEngine.Transform();
    public Inventory Inventory = new Inventory(2, 3);
    public int Requests;
    public bool Busy;

    public Inventory GetInventory()
    {
        return Inventory;
    }

    public bool IsInUse()
    {
        return Busy;
    }

    public void StackAll()
    {
        Requests++;
    }

    public void RPC_OpenResponse()
    {
    }

    public void RPC_StackResponse()
    {
    }
}

public class Wagon
{
    public bool InUse()
    {
        return false;
    }
}

public class ZNetView
{
    public bool Owner = true;

    public bool IsOwner()
    {
        return Owner;
    }

    public bool IsValid()
    {
        return true;
    }

    public ZDO GetZDO()
    {
        return new ZDO();
    }
}

public class ZDO
{
    public int GetInt(int key, int fallback)
    {
        return fallback;
    }
}

public static class ZDOVars
{
    public const int s_inUse = 1;
}

namespace HarmonyLib
{
    public class HarmonyPatch : Attribute
    {
        public HarmonyPatch(Type type)
        {
        }

        public HarmonyPatch(string name)
        {
        }
    }

    public class HarmonyPrefix : Attribute
    {
    }

    public class HarmonyPostfix : Attribute
    {
    }
}

namespace HexQuickStackStorage
{
    public static class Plugin
    {
        public const float SearchRadius = 25;
        public const bool EnableAutoStoreTrophies = true;

        public static bool EnableChestAutoSorting;
        public static Logger Log = new Logger();
    }

    public class Logger
    {
        public void LogWarning(string text)
        {
        }
    }

    public static class FavoriteService
    {
        public static HashSet<ItemDrop.ItemData> Favorites = new HashSet<ItemDrop.ItemData>();

        public static bool IsFavorite(ItemDrop.ItemData item)
        {
            return Favorites.Contains(item);
        }
    }

    public static class ItemStateService
    {
        public static bool IsHotbarItem(ItemDrop.ItemData item)
        {
            return item.m_gridPos.y == 0;
        }
    }

    public static class ContainerService
    {
        public static List<Container> Nearby = new List<Container>();
        public static bool Allowed = true;

        public static List<Container> GetNearbyContainers(Player player)
        {
            return Nearby;
        }

        public static bool CanUseContainer(Container container)
        {
            return Allowed;
        }
    }
}

namespace HexQuickStackStorage.UI
{
    public static class InventoryUiController
    {
        public static Container GetCurrentContainer()
        {
            return null;
        }
    }
}

namespace HexQuickStackStorage.Services
{
    public static class InventoryOperationsService
    {
        public static void NotifyChanged(Inventory inventory)
        {
        }

        public static ItemDrop.ItemData FindFreeStackItem(Inventory inventory, ItemDrop.ItemData item)
        {
            return inventory.Items.Find(i => i.m_shared.m_name == item.m_shared.m_name && i.m_stack < i.m_shared.m_maxStackSize);
        }

        public static Vector2i FindEmptySlot(Inventory inventory, ItemDrop.ItemData item)
        {
            for (int y = 0; y < inventory.Height; y++)
            {
                for (int x = 0; x < inventory.Width; x++)
                {
                    if (!inventory.Items.Exists(i => i.m_gridPos.x == x && i.m_gridPos.y == y))
                    {
                        return new Vector2i(x, y);
                    }
                }
            }

            return new Vector2i(-1, -1);
        }
    }
}
