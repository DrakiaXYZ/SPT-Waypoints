using BepInEx;
using DrakiaXYZ.BigBrain.Brains;
using DrakiaXYZ.Waypoints.BrainLogic;
using DrakiaXYZ.Waypoints.Helpers;
using DrakiaXYZ.Waypoints.Patches;
using DrakiaXYZ.Waypoints.VersionChecker;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace DrakiaXYZ.Waypoints
{
    [BepInPlugin("xyz.drakia.waypoints", "DrakiaXYZ-Waypoints", "1.1.0")]
    public class WaypointsPlugin : BaseUnityPlugin
    {
        public static string PluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string CustomFolder = Path.Combine(PluginFolder, "custom");
        public static string MeshFolder = Path.Combine(PluginFolder, "mesh");
        public static string PointsFolder = Path.Combine(PluginFolder, "points");
        public static string NavMeshFolder = Path.Combine(PluginFolder, "navmesh");

        private void Awake()
        {
            if (!TarkovVersion.CheckEftVersion(Logger, Info, Config))
            {
                throw new Exception($"Invalid EFT Version");
            }

            Settings.Init(Config);

            // Make sure plugin folders exist
            Directory.CreateDirectory(CustomFolder);

            try
            {
                CustomWaypointLoader.Instance.loadData();

                new DebugPatch().Enable();
                new WaypointPatch().Enable();
                new BotOwnerRunPatch().Enable();

                new EditorPatch().Enable();

                new DoorBlockerPatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError($"{GetType().Name}: {ex}");
                throw;
            }

// Note: We only include this in debug builds for now, because we're not shipping BigBrain
#if DEBUG
            BrainManager.AddCustomLayer(typeof(RoamingLayer), new List<string>() { "Assault", "PMC" }, 1);
#endif
        }
    }
}
