using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using DrakiaXYZ.Waypoints.Patches;
using System;
using UnityEngine;

namespace DrakiaXYZ.Waypoints
{
    [BepInPlugin("xyz.drakia.waypoints", "DrakiaXYZ-Waypoints", "0.0.1")]
    public class WaypointsPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> DebugEnabled;
        public static ConfigEntry<bool> ShowSubPoints;

        private void Awake()
        {
            DebugEnabled = Config.Bind(
                "Main",
                "Debug",
                false,
                "Whether to draw debug objects in-world");
            DebugEnabled.SettingChanged += DebugEnabled_SettingChanged;

            ShowSubPoints = Config.Bind(
                "Main",
                "ShowSubPoints",
                true,
                "When debug is enabled, show sub-points");
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

        public WaypointsPlugin()
        {
            Logger.LogInfo("Loading: DrakiaXYZ-Waypoints");

            try
            {
                CustomWaypointLoader.Instance.loadData();
                new DebugPatch().Enable();
                new WaypointPatch().Enable();
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
