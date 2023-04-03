using BepInEx;
using DrakiaXYZ.Waypoints.Helpers;
using DrakiaXYZ.Waypoints.Patches;
using System;
using System.IO;

namespace DrakiaXYZ.Waypoints
{
    [BepInPlugin("xyz.drakia.waypoints", "DrakiaXYZ-Waypoints", "1.0.0")]
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

            try
            {
                CustomWaypointLoader.Instance.loadData();

                new DebugPatch().Enable();
                new WaypointPatch().Enable();
                new BotOwnerRunPatch().Enable();

                new EditorPatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError($"{GetType().Name}: {ex}");
                throw;
            }
        }
    }
}
