using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using DrakiaXYZ.Waypoints.Components;
using DrakiaXYZ.Waypoints.Patches;
using System;
using System.IO;
using UnityEngine;

namespace DrakiaXYZ.Waypoints
{
    [BepInPlugin("xyz.drakia.waypoints", "DrakiaXYZ-Waypoints", "0.0.1")]
    public class WaypointsPlugin : BaseUnityPlugin
    {
        public static string PluginFolder = "BepInEx\\plugins\\DrakiaXYZ-Waypoints";
        public static string CustomFolder = $"{PluginFolder}\\custom";
        public static string MeshFolder = $"{PluginFolder}\\mesh";
        public static string PointsFolder = $"{PluginFolder}\\points";

        private const string DebugSectionTitle = "Debug";
        private const string ExportSectionTitle = "Export (Requires Debug)";
        private const string EditorSectionTitle = "Editor";

        public static ConfigEntry<bool> DebugEnabled;
        public static ConfigEntry<bool> DrawGizmos;
        public static ConfigEntry<bool> ShowNavMesh;
        public static ConfigEntry<float> NavMeshOffset;

        public static ConfigEntry<bool> ExportNavMesh;
        public static ConfigEntry<bool> ExportMapPoints;

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

            DrawGizmos = Config.Bind(
                DebugSectionTitle,
                "DrawGizmos",
                false,
                "Whether to draw gizmos when debug is enabled");

            ShowNavMesh = Config.Bind(
                DebugSectionTitle,
                "ShowNavMesh",
                false,
                "Whether to show the navigation mesh");
            ShowNavMesh.SettingChanged += ShowNavMesh_SettingChanged;

            NavMeshOffset = Config.Bind(
                DebugSectionTitle,
                "NavMeshOffset",
                0.02f,
                "The amount to offset the nav mesh so it's more visible over the terrain");
            NavMeshOffset.SettingChanged += NavMeshOffset_SettingChanged;

            ExportNavMesh = Config.Bind(
                ExportSectionTitle,
                "ExportNavMesh",
                false,
                "Whether to export the nav mesh on map load");

            ExportMapPoints = Config.Bind(
                ExportSectionTitle,
                "ExportMapPoints",
                false,
                "Whether to export map points on map load (Waypoints)");

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

            // Make sure plugin folders exist
            Directory.CreateDirectory(PluginFolder);
            Directory.CreateDirectory(CustomFolder);
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
                BotZoneDebugComponent.Enable();
            }
            else
            {
                BotZoneDebugComponent.Disable();
            }
        }

        private void ShowNavMesh_SettingChanged(object sender, EventArgs e)
        {
            // If no game, do nothing
            if (!Singleton<IBotGame>.Instantiated)
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

        private void NavMeshOffset_SettingChanged(object sender, EventArgs e)
        {
            if (ShowNavMesh.Value)
            {
                NavMeshDebugComponent.Disable();
                NavMeshDebugComponent.Enable();
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


                //new PatrollingDataManualUpdatePatch().Enable();
                //new IsComePatch().Enable();
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
