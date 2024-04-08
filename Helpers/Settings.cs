using BepInEx.Configuration;
using Comfort.Common;
using DrakiaXYZ.Waypoints.Components;
using System;

namespace DrakiaXYZ.Waypoints.Helpers
{
    internal class Settings
    {
        private const string GeneralSectionTitle = "General";
        private const string DebugSectionTitle = "Debug";
        private const string ExportSectionTitle = "Export (Requires Debug)";

        public static ConfigEntry<bool> EnableCustomNavmesh;

        public static ConfigEntry<bool> DebugEnabled;
        public static ConfigEntry<bool> ShowNavMesh;
        public static ConfigEntry<float> NavMeshOffset;

        public static ConfigEntry<bool> ExportNavMesh;

        public static void Init(ConfigFile Config)
        {
            EnableCustomNavmesh = Config.Bind(
                GeneralSectionTitle,
                "EnableCustomNavmesh",
                true,
                "Whether to use custom nav meshes when available"
                );

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
    }
}
