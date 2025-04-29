using SPT.Reflection.Patching;
using BepInEx;
using DrakiaXYZ.Helpers;
using DrakiaXYZ.Waypoints.Helpers;
using DrakiaXYZ.Waypoints.Patches;
using DrakiaXYZ.Waypoints.VersionChecker;
using EFT;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using HarmonyLib;
using BepInEx.Logging;

namespace DrakiaXYZ.Waypoints
{
    [BepInPlugin("xyz.drakia.waypoints", "DrakiaXYZ-Waypoints", "1.7.0")]
    [BepInDependency("com.SPT.core", "3.11.0")]
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
                //new GroupPointCachePatch().Enable();
                //new BotVoxelesPersonalActivatePatch().Enable();
                //new GroupPointGetByIdPatch().Enable();

                // Debug perf timing output
                //new PerfTimingPatch().Enable();
            }
            catch (Exception ex)
            {
                Logger.LogError($"{GetType().Name}: {ex}");
                throw;
            }
        }

        public class PerfTimingPatch
        {
            protected static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(ModulePatch));

            public void Enable()
            {
                var harmony = new Harmony("xyz.drakia.waypoints");

                //var props = AccessTools.GetDeclaredProperties(typeof(BotOwner));
                //foreach (var prop in props)
                //{
                //    var method = prop.PropertyType.GetMethod("Activate");
                //    if (method != null && !method.IsAbstract)
                //    {
                //        Logger.LogDebug($"Adding timing to {prop.PropertyType.Name}::{method.Name}");
                //        var target = method;
                //        var prefix = new HarmonyMethod(typeof(PerfTimingPatch).GetMethod("PatchPrefix"));
                //        var postfix = new HarmonyMethod(typeof(PerfTimingPatch).GetMethod("PatchPostfix"));
                //        harmony.Patch(target, prefix, postfix);
                //    }
                //}

                // Time the overall activate method
                {
                    var target = AccessTools.Method(typeof(BotOwner), nameof(BotOwner.method_10));
                    var prefix = new HarmonyMethod(typeof(PerfTimingPatch).GetMethod("PatchPrefix"));
                    var postfix = new HarmonyMethod(typeof(PerfTimingPatch).GetMethod("PatchPostfix"));
                    harmony.Patch(target, prefix, postfix);
                }
            }

            [PatchPrefix]
            public static void PatchPrefix(out Stopwatch __state)
            {
                __state = new Stopwatch();
                __state.Start();
            }

            [PatchPostfix]
            public static void PatchPostfix(object __instance, Stopwatch __state)
            {
                __state.Stop();
                Logger.LogDebug($"{__instance.GetType()} Activate took {__state.ElapsedMilliseconds}ms");
            }
        }
    }
}
