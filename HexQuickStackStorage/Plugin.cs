using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
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
        private const string PluginVersion = "1.4.1";

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
        private static ConfigEntry<bool> _lockConfiguration;

        private Harmony _harmonyInstance;
        private bool _isValidatingModifierKeys;
        private bool _modifierValidationMessagePending;

        internal static float SearchRadius => Instance?._searchRadius != null ? Instance._searchRadius.Value : 25f;
        internal static KeyboardShortcut QuickStackShortcut => Instance?._quickStackShortcut != null ? Instance._quickStackShortcut.Value : new KeyboardShortcut(KeyCode.P);
        internal static KeyCode TrashModifierKey => Instance?._trashModifierKey != null ? Instance._trashModifierKey.Value : DefaultTrashModifierKey;
        internal static KeyCode FavoriteModifierKey => Instance?._favoriteModifierKey != null ? Instance._favoriteModifierKey.Value : DefaultFavoriteModifierKey;
        internal static bool EnableChestAutoSorting => Instance?._enableChestAutoSorting != null && Instance._enableChestAutoSorting.Value;
        internal static bool EnableAutoStoreTrophies => Instance?._enableAutoStoreTrophies != null && Instance._enableAutoStoreTrophies.Value;
        internal static ContainerAccessModeEnum ContainerAccessMode => Instance?._containerAccessMode != null ? Instance._containerAccessMode.Value : ContainerAccessModeEnum.CharacterOwned;
        internal static bool EnableDeleteConfirmation => Instance?._enableDeleteConfirmation != null && Instance._enableDeleteConfirmation.Value;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            _lockConfiguration = Config.Bind(
                "Server",
                "LockConfiguration",
                false,
                "Locks synchronized configuration settings to the server's values."
            );

            ConfigSync.AddLockingConfigEntry(_lockConfiguration);

            _searchRadius = Config.Bind(
                "Chests",
                "SearchRadius",
                25f,
                new ConfigDescription(
                    "The radius in which to search for nearby containers when quick stacking.",
                    new AcceptableValueRange<float>(5f, 150f)
                )
            );

            ConfigSync.AddConfigEntry(_searchRadius);

            _quickStackShortcut = Config.Bind(
                "Chests",
                "QuickStackShortcut",
                new KeyboardShortcut(KeyCode.P),
                "Keyboard shortcut used to quick stack nearby containers."
            );

            _trashModifierKey = Config.Bind(
                "Inventory",
                "TrashModifierKey",
                DefaultTrashModifierKey,
                "Modifier key held while right-clicking an item to mark or unmark it as trash."
            );

            _favoriteModifierKey = Config.Bind(
                "Inventory",
                "FavoriteModifierKey",
                DefaultFavoriteModifierKey,
                "Modifier key held while right-clicking an item to favorite or unfavorite it."
            );

            _enableChestAutoSorting = Config.Bind(
                "Chests",
                "EnableChestAutoSorting",
                false,
                "Automatically sort a chest when it is opened."
            );

            ConfigSync.AddConfigEntry(_enableChestAutoSorting);

            _enableAutoStoreTrophies = Config.Bind(
                "Chests",
                "EnableAutoStoreTrophies",
                false,
                "Automatically store trophies when quick stacking into containers that already contain any trophy."
            );

            ConfigSync.AddConfigEntry(_enableAutoStoreTrophies);

            _containerAccessMode = Config.Bind(
                "Chests",
                "ContainerAccessMode",
                ContainerAccessModeEnum.CharacterOwned,
                "Controls which containers Quick Stack can use. CharacterOwned only uses containers created by the current character. Accessible allows any public container the character can access."
            );

            ConfigSync.AddConfigEntry(_containerAccessMode);

            _enableDeleteConfirmation = Config.Bind(
                "Inventory",
                "EnableDeleteConfirmation",
                true,
                "Enable a confirmation dialog when deleting items marked as trash."
            );

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

            if (!QuickStackShortcut.IsDown())
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

            _favoriteModifierKey.Value = _trashModifierKey.Value == DefaultFavoriteModifierKey
                ? DefaultTrashModifierKey
                : DefaultFavoriteModifierKey;

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
            player.Message(
                MessageHud.MessageType.Center,
                "Trash and Favorite modifier keys cannot be the same. Favorite modifier has been reset.",
                0,
                null
            );

            _modifierValidationMessagePending = false;
        }
    }
}