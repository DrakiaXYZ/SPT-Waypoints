using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using DrakiaXYZ.Waypoints.Components;
using DrakiaXYZ.Waypoints.Patches;
using System;
using UnityEngine;

namespace DrakiaXYZ.Waypoints
{
    [BepInPlugin("xyz.drakia.waypoints", "DrakiaXYZ-Waypoints", "0.0.1")]
    public class WaypointsPlugin : BaseUnityPlugin
    {
        private const string DebugSectionTitle = "Debug";
        private const string EditorSectionTitle = "Editor";

        public static ConfigEntry<bool> DebugEnabled;
        public static ConfigEntry<bool> ShowNavMesh;

        public static ConfigEntry<bool> EditorEnabled;
        public static ConfigEntry<KeyboardShortcut> AddWaypointKey;
        public static ConfigEntry<KeyboardShortcut> RemoveWaypointKey;

        private void Awake()
        {
            DebugEnabled = Config.Bind(
                DebugSectionTitle,
                "Debug",
                false,
                "Whether to draw debug objects in-world");
            DebugEnabled.SettingChanged += DebugEnabled_SettingChanged;

            ShowNavMesh = Config.Bind(
                DebugSectionTitle,
                "ShowNavMesh",
                false,
                "Whether to show the navigation mesh");
            ShowNavMesh.SettingChanged += ShowNavMesh_SettingChanged;

            EditorEnabled = Config.Bind(
                EditorSectionTitle,
                "EditorEnabled",
                false,
                new ConfigDescription(
                    "Whether to enable editing mode",
                    null,
                    new ConfigurationManagerAttributes { Order = 1 }));
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
            // If no game, do nothing
            if (!Singleton<IBotGame>.Instantiated)
            {
                return;
            }

            if (DebugEnabled.Value)
            {
                DebugPatch.EnableDebug();
            }
            else
            {
                DebugPatch.DisableDebug();
            }
        }

        private void ShowNavMesh_SettingChanged(object sender, EventArgs e)
        {
            // If no game, do nothing
            if (!Singleton<IBotGame>.Instantiated)
            {
                return;
            }

            // If debug isn't enabled, don't do anything
            if (!DebugEnabled.Value)
            {
                return;
            }

            if (ShowNavMesh.Value)
            {
                NavMeshDebugComponent.Enable();
            }
            else
            {
                NavMeshDebugComponent.Disable();
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
