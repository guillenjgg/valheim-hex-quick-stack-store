using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HexQuickStackStorage
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        private const string PluginGuid = "com.hex.quickstackstorage";
        private const string PluginName = "HexQuickStackStorage";
        private const string PluginVersion = "1.0.0";

        private ConfigEntry<float> _searchRadius;
        private ConfigEntry<KeyboardShortcut> _quickStackShortcut;
        private Harmony _harmonyInstance;

        internal static float SearchRadius => Instance?._searchRadius != null ? Instance._searchRadius.Value : 25f;
        internal static KeyboardShortcut QuickStackShortcut => Instance?._quickStackShortcut != null ? Instance._quickStackShortcut.Value : new KeyboardShortcut(KeyCode.P);

        internal static ManualLogSource Log;
        internal static Plugin Instance;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            _searchRadius = Config.Bind(
                "General",
                "SearchRadius",
                25f,
                new ConfigDescription(
                    "The radius in which to search for nearby containers when quick stacking.",
                    new AcceptableValueRange<float>(5f, 150f)));

            _quickStackShortcut = Config.Bind(
                "General",
                "QuickStackShortcut",
                new KeyboardShortcut(KeyCode.P),
                "Keyboard shortcut used to quick stack nearby containers.");

            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmonyInstance = new Harmony(PluginGuid);
            _harmonyInstance.PatchAll(assembly);

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }

        private void Update()
        {
            if (IsTypingInInputField())
            {
                return;
            }

            if (!QuickStackShortcut.IsDown())
            {
                return;
            }

            Player player = Player.m_localPlayer;

            if (player == null)
            {
                return;
            }

            QuickStackService.QuickStack(player);
        }

        private void OnDestroy()
        {
            Log.LogInfo($"{PluginName} v{PluginVersion} unloaded.");

            _harmonyInstance?.UnpatchSelf();
            _harmonyInstance = null;
            Instance = null;
            Log = null;
        }

        private static bool IsTypingInInputField()
        {
            if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
            {
                return false;
            }

            GameObject selectedObject = EventSystem.current.currentSelectedGameObject;

            return selectedObject.GetComponent<InputField>() != null || selectedObject.GetComponent<TMP_InputField>() != null;
        }
    }
}