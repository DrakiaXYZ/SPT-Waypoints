using BepInEx;
using DrakiaXYZ.Helpers;
using DrakiaXYZ.Waypoints.Helpers;
using DrakiaXYZ.Waypoints.Patches;
using DrakiaXYZ.Waypoints.VersionChecker;
using System;
using System.IO;
using System.Reflection;

namespace DrakiaXYZ.Waypoints
{
    [BepInPlugin("xyz.drakia.waypoints", "DrakiaXYZ-Waypoints", "1.4.1")]
    public class WaypointsPlugin : BaseUnityPlugin
    {
        public static string PluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string MeshFolder = Path.Combine(PluginFolder, "mesh");
        public static string PointsFolder = Path.Combine(PluginFolder, "points");
        public static string NavMeshFolder = Path.Combine(PluginFolder, "navmesh");

        private void Awake()
        {
            if (!TarkovVersion.CheckEftVersion(Logger, Info, Config))
            {
                throw new Exception($"Invalid EFT Version");
            }

            if (!DependencyChecker.ValidateDependencies(Logger, Info, this.GetType(), Config))
            {
                throw new Exception($"Missing Dependencies");
            }

            Settings.Init(Config);

            try
            {
                new DebugPatch().Enable();
                new WaypointPatch().Enable();
                new DoorLinkPatch().Enable();
                new DoorLinkStateChangePatch().Enable();
                new SwitchDoorBlockerPatch().Enable();
                new ExfilDoorBlockerPatch().Enable();
                new FindPathPatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError($"{GetType().Name}: {ex}");
                throw;
            }
        }
    }
}
