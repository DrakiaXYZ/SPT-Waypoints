using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using DrakiaXYZ.Waypoints.Components;
using DrakiaXYZ.Waypoints.Patches;
using EFT;
using System;
using UnityEngine;

namespace DrakiaXYZ.Waypoints
{
    [BepInPlugin("xyz.drakia.waypoints", "DrakiaXYZ-Waypoints", "0.0.1")]
    public class WaypointsPlugin : BaseUnityPlugin
    {
        private const string MainSectionTitle = "Main";
        private const string EditorSectionTitle = "Editor";

        public static ConfigEntry<bool> DebugEnabled;
        public static ConfigEntry<bool> ShowSubPoints;

        public static ConfigEntry<bool> EditorEnabled;
        public static ConfigEntry<KeyboardShortcut> AddWaypointKey;
        public static ConfigEntry<KeyboardShortcut> RemoveWaypointKey;

        private void Awake()
        {
            DebugEnabled = Config.Bind(
                MainSectionTitle,
                "Debug",
                false,
                "Whether to draw debug objects in-world");
            DebugEnabled.SettingChanged += DebugEnabled_SettingChanged;

            ShowSubPoints = Config.Bind(
                MainSectionTitle,
                "ShowSubPoints",
                true,
                "When debug is enabled, show sub-points");

            EditorEnabled = Config.Bind(
                EditorSectionTitle,
                "EditorEnabled",
                false,
                "Whether to enable editing mode");
            EditorEnabled.SettingChanged += EditorEnabled_SettingChanged;

            AddWaypointKey = Config.Bind(
                EditorSectionTitle,
                "AddWaypoint",
                new KeyboardShortcut(KeyCode.KeypadPlus),
                "Add a Waypoint at the current position");

            RemoveWaypointKey = Config.Bind(
                EditorSectionTitle,
                "RemoveWaypoint",
                new KeyboardShortcut(KeyCode.KeypadMinus),
                "Remove the nearest Waypoint added this session");
        }

        private void DebugEnabled_SettingChanged(object sender, EventArgs e)
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                if (DebugEnabled.Value)
                {
                    DebugPatch.EnableDebug();
                }
                else
                {
                    DebugPatch.DisableDebug();
                }
            }
        }

        private void EditorEnabled_SettingChanged(object sender, EventArgs e)
        {
            if (EditorEnabled.Value)
            {
                EditorComponent.Enable();
            }
            else
            {
                EditorComponent.Disable();
            }
        }

        public WaypointsPlugin()
        {
            Logger.LogInfo("Loading: DrakiaXYZ-Waypoints");

            try
            {
                CustomWaypointLoader.Instance.loadData();
                new DebugPatch().Enable();
                new WaypointPatch().Enable();
                new EditorPatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError($"{GetType().Name}: {ex}");
                throw;
            }

            Logger.LogInfo("Completed: DrakiaXYZ-Waypoints");
        }
    }
}
