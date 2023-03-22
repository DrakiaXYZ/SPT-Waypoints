using BepInEx.Configuration;
using Comfort.Common;
using DrakiaXYZ.Waypoints.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DrakiaXYZ.Waypoints.Helpers
{
    internal class Settings
    {
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

        public static void Init(ConfigFile Config)
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
                new ConfigDescription(
                    "The amount to offset the nav mesh so it's more visible over the terrain",
                    new AcceptableValueRange<float>(0f, 2f)
                ));
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
                new ConfigDescription(
                    "Add a Waypoint at the current position",
                    null,
                    new ConfigurationManagerAttributes { Order = 2 }));

            RemoveWaypointKey = Config.Bind(
                EditorSectionTitle,
                "RemoveWaypoint",
                new KeyboardShortcut(KeyCode.KeypadMinus),
                new ConfigDescription(
                    "Remove the nearest Waypoint added this session",
                    null,
                    new ConfigurationManagerAttributes { Order = 3 }));
        }

        private static void DebugEnabled_SettingChanged(object sender, EventArgs e)
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

        private static void ShowNavMesh_SettingChanged(object sender, EventArgs e)
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

        private static void NavMeshOffset_SettingChanged(object sender, EventArgs e)
        {
            if (ShowNavMesh.Value)
            {
                NavMeshDebugComponent.Disable();
                NavMeshDebugComponent.Enable();
            }
        }

        private static void EditorEnabled_SettingChanged(object sender, EventArgs e)
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
    }
}
