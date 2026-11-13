using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HexQuickStackStorage.UI;
using ServerSync;
using System;
using System.Reflection;
using UnityEngine;

namespace HexQuickStackStorage
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        internal const string PluginGuid = "com.hex.quickstackstorage";

        private const string PluginName = "HexQuickStackStorage";
        private const string PluginVersion = "1.4.5";
        private const float DefaultSearchRadius = 25f;
        private const float MinimumSearchRadius = 5f;
        private const float MaximumSearchRadius = 150f;
        private const string UiLayoutSection = "UI Layout";
        private const float MinimumLayoutOffset = -500f;
        private const float MaximumLayoutOffset = 500f;

        private static readonly Vector2 DefaultActionButtonContainerOffset = new Vector2(6f, 0f);

        private static readonly ConfigSync ConfigSync = new ConfigSync(PluginGuid)
        {
            DisplayName = PluginName,
            CurrentVersion = PluginVersion,
            MinimumRequiredVersion = PluginVersion
        };

        private const KeyCode DefaultTrashModifierKey = KeyCode.LeftShift;
        private const KeyCode DefaultFavoriteModifierKey = KeyCode.LeftControl;

        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }

        private ConfigEntry<float> _searchRadius;
        private ConfigEntry<KeyboardShortcut> _quickStackShortcut;
        private ConfigEntry<KeyCode> _trashModifierKey;
        private ConfigEntry<KeyCode> _favoriteModifierKey;
        private ConfigEntry<bool> _enableChestAutoSorting;
        private ConfigEntry<bool> _enableAutoStoreTrophies;
        private ConfigEntry<ContainerAccessModeEnum> _containerAccessMode;
        private ConfigEntry<bool> _enableDeleteConfirmation;
        private ConfigEntry<bool> _lockConfiguration;
        private ConfigEntry<bool> _enableTrashFeature;
        private ConfigEntry<float> _actionButtonContainerXOffset;
        private ConfigEntry<float> _actionButtonContainerYOffset;
        private ConfigEntry<float> _trashButtonXOffset;
        private ConfigEntry<float> _trashButtonYOffset;
        private Harmony _harmonyInstance;
        private bool _isValidatingModifierKeys;
        private bool _modifierValidationMessagePending;

        internal static float SearchRadius => Instance?._searchRadius != null ? Instance._searchRadius.Value : DefaultSearchRadius;
        internal static KeyboardShortcut QuickStackShortcut => Instance?._quickStackShortcut != null ? Instance._quickStackShortcut.Value : new KeyboardShortcut(KeyCode.P);
        internal static KeyCode TrashModifierKey => Instance?._trashModifierKey != null ? Instance._trashModifierKey.Value : DefaultTrashModifierKey;
        internal static KeyCode FavoriteModifierKey => Instance?._favoriteModifierKey != null ? Instance._favoriteModifierKey.Value : DefaultFavoriteModifierKey;
        internal static bool EnableChestAutoSorting => Instance?._enableChestAutoSorting != null && Instance._enableChestAutoSorting.Value;
        internal static bool EnableAutoStoreTrophies => Instance?._enableAutoStoreTrophies != null && Instance._enableAutoStoreTrophies.Value;
        internal static ContainerAccessModeEnum ContainerAccessMode => Instance?._containerAccessMode != null ? Instance._containerAccessMode.Value : ContainerAccessModeEnum.CharacterOwned;
        internal static bool EnableDeleteConfirmation => Instance?._enableDeleteConfirmation != null && Instance._enableDeleteConfirmation.Value;
        internal static bool EnableTrashFeature => Instance?._enableTrashFeature != null && Instance._enableTrashFeature.Value;
        internal static Vector2 ActionButtonContainerOffset => new Vector2(Instance?._actionButtonContainerXOffset?.Value ?? DefaultActionButtonContainerOffset.x, Instance?._actionButtonContainerYOffset?.Value ?? DefaultActionButtonContainerOffset.y);
        internal static Vector2 TrashButtonOffset => new Vector2(Instance?._trashButtonXOffset?.Value ?? 0f, Instance?._trashButtonYOffset?.Value ?? 0f);

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            _lockConfiguration = Config.Bind("Server", "LockConfiguration", false, "Locks synchronized configuration settings to the server's values.");

            ConfigSync.AddLockingConfigEntry(_lockConfiguration);

            _searchRadius = Config.Bind("Chests", "SearchRadius", DefaultSearchRadius, new ConfigDescription("The radius in which to search for nearby containers when quick stacking.", new AcceptableValueRange<float>(MinimumSearchRadius, MaximumSearchRadius)));

            ConfigSync.AddConfigEntry(_searchRadius);

            _quickStackShortcut = Config.Bind("Chests", "QuickStackShortcut", new KeyboardShortcut(KeyCode.P), "Keyboard shortcut used to quick stack nearby containers.");
            _trashModifierKey = Config.Bind("Inventory", "TrashModifierKey", DefaultTrashModifierKey, "Modifier key held while right-clicking an item to mark or unmark it as trash.");
            _favoriteModifierKey = Config.Bind("Inventory", "FavoriteModifierKey", DefaultFavoriteModifierKey, "Modifier key held while right-clicking an item to favorite or unfavorite it.");
            _enableChestAutoSorting = Config.Bind("Chests", "EnableChestAutoSorting", false, "Automatically sort a chest when it is opened.");

            ConfigSync.AddConfigEntry(_enableChestAutoSorting);

            _enableAutoStoreTrophies = Config.Bind("Chests", "EnableAutoStoreTrophies", false, "Automatically store trophies when quick stacking into containers that already contain any trophy.");

            ConfigSync.AddConfigEntry(_enableAutoStoreTrophies);

            _containerAccessMode = Config.Bind("Chests", "ContainerAccessMode", ContainerAccessModeEnum.CharacterOwned, "Controls which containers Quick Stack can use. CharacterOwned only uses containers created by the current character. Accessible allows any public container the character can access.");

            ConfigSync.AddConfigEntry(_containerAccessMode);

            _enableDeleteConfirmation = Config.Bind("Inventory", "EnableDeleteConfirmation", true, "Enable a confirmation dialog when deleting items marked as trash.");
            _enableTrashFeature = Config.Bind("Inventory", "EnableTrashFeature", true, "Enable the trash feature, allowing items to be marked as trash and deleted. If disabled the trash tab on the player inventory will be removed.. Requires game restart.");
            BindLayoutOffset("S/Q Container", DefaultActionButtonContainerOffset, out _actionButtonContainerXOffset, out _actionButtonContainerYOffset);
            BindLayoutOffset("Trash Button", Vector2.zero, out _trashButtonXOffset, out _trashButtonYOffset);

            _trashModifierKey.SettingChanged += OnModifierKeyChanged;
            _favoriteModifierKey.SettingChanged += OnModifierKeyChanged;

            ValidateModifierKeys();
            TrashService.Initialize();
            FavoriteService.Initialize();

            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmonyInstance = new Harmony(PluginGuid);

            _harmonyInstance.PatchAll(assembly);
            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }

        private void BindLayoutOffset(string settingPrefix, Vector2 defaultOffset, out ConfigEntry<float> xOffset, out ConfigEntry<float> yOffset)
        {
            // Read the previous vector setting before replacing it with individual axis sliders.
            ConfigEntry<Vector2> legacyOffset = Config.Bind(UiLayoutSection, settingPrefix + " Offset", defaultOffset);
            Vector2 savedOffset = legacyOffset.Value;

            Config.Remove(legacyOffset.Definition);

            ConfigDescription xDescription = new ConfigDescription("Horizontal offset from the normal position. Positive moves right; negative moves left. Updates immediately while the inventory is open. Client-only.", new AcceptableValueRange<float>(MinimumLayoutOffset, MaximumLayoutOffset));
            ConfigDescription yDescription = new ConfigDescription("Vertical offset from the normal position. Positive moves up; negative moves down. Updates immediately while the inventory is open. Client-only.", new AcceptableValueRange<float>(MinimumLayoutOffset, MaximumLayoutOffset));
            xOffset = Config.Bind(UiLayoutSection, settingPrefix + " X Offset", savedOffset.x, xDescription);
            yOffset = Config.Bind(UiLayoutSection, settingPrefix + " Y Offset", savedOffset.y, yDescription);
        }

        private void LateUpdate()
        {
            InventoryUiController.RefreshActionButtonAnchors();
            InventoryUiController.RefreshTrashButtonOffset();
        }

        private void Update()
        {
            Player player = Player.m_localPlayer;

            if (player == null)
            {
                return;
            }

            if (_modifierValidationMessagePending)
            {
                ShowModifierValidationMessage(player);
            }

            if (!IsShortcutDown(_quickStackShortcut.Value))
            {
                return;
            }

            if (global::Console.IsVisible())
            {
                return;
            }

            if (TextInput.IsVisible())
            {
                return;
            }

            if (Menu.IsVisible())
            {
                return;
            }

            if (InventoryUiController.GetCurrentContainer() != null)
            {
                return;
            }

            QuickStackService.QuickStack(player);
        }

        internal static void SetDeleteConfirmationEnabled(bool enabled)
        {
            if (Instance?._enableDeleteConfirmation == null)
            {
                return;
            }

            Instance._enableDeleteConfirmation.Value = enabled;
        }

        private void OnDestroy()
        {
            QuickStackService.ClearPendingRequests();

            if (_trashModifierKey != null)
            {
                _trashModifierKey.SettingChanged -= OnModifierKeyChanged;
            }

            if (_favoriteModifierKey != null)
            {
                _favoriteModifierKey.SettingChanged -= OnModifierKeyChanged;
            }

            _harmonyInstance?.UnpatchSelf();
            Log?.LogInfo($"{PluginName} v{PluginVersion} unloaded.");

            Log = null;
            Instance = null;
        }

        private void OnModifierKeyChanged(object sender, EventArgs e)
        {
            ValidateModifierKeys();
        }

        private void ValidateModifierKeys()
        {
            if (_isValidatingModifierKeys)
            {
                return;
            }

            if (_trashModifierKey.Value != _favoriteModifierKey.Value)
            {
                return;
            }

            _isValidatingModifierKeys = true;
            _favoriteModifierKey.Value = _trashModifierKey.Value == DefaultFavoriteModifierKey ? DefaultTrashModifierKey : DefaultFavoriteModifierKey;
            _isValidatingModifierKeys = false;
            Player player = Player.m_localPlayer;

            if (player == null)
            {
                _modifierValidationMessagePending = true;

                return;
            }

            ShowModifierValidationMessage(player);
        }

        private void ShowModifierValidationMessage(Player player)
        {
            player.Message(MessageHud.MessageType.Center, "Trash and Favorite modifier keys cannot be the same. Favorite modifier has been reset.", 0, null);

            _modifierValidationMessagePending = false;
        }

        private static bool IsShortcutDown(KeyboardShortcut shortcut)
        {
            if (!ZInput.GetKeyDown(shortcut.MainKey))
            {
                return false;
            }

            foreach (KeyCode modifier in shortcut.Modifiers)
            {
                if (!ZInput.GetKey(modifier))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
