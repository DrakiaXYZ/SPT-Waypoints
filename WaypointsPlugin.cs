using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using DrakiaXYZ.Waypoints.Components;
using DrakiaXYZ.Waypoints.Helpers;
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

        private void Awake()
        {
            Settings.Init(Config);

            // Make sure plugin folders exist
            Directory.CreateDirectory(PluginFolder);
            Directory.CreateDirectory(CustomFolder);
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
                //new PatrollingDataComeToPointPatch().Enable();
                new GClass479FindNextPointPatch().Enable();
                //new PatrollingDataPointChooserPatch().Enable();
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
