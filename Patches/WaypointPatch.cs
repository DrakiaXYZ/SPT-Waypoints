using SPT.Reflection.Patching;
using Comfort.Common;
using DrakiaXYZ.Waypoints.Helpers;
using EFT;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace DrakiaXYZ.Waypoints.Patches
{
    public class WaypointPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsController), nameof(BotsController.Init));
        }

        /// <summary>
        /// 
        /// </summary>
        [PatchPrefix]
        private static void PatchPrefix(BotsController __instance, BotZone[] botZones)
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null)
            {
                Logger.LogError("BotController::Init called, but GameWorld doesn't exist");
                return;
            }

            if (Settings.EnableCustomNavmesh.Value)
            {
                InjectNavmesh(gameWorld);
            }
        }

        private static void InjectNavmesh(GameWorld gameWorld)
        {
            // First we load the asset from the bundle
            string mapName = gameWorld.LocationId.ToLower();

            // Standardize Factory
            if (mapName.StartsWith("factory4"))
            {
                mapName = "factory4";
            }
            // Standardize Ground Zero
            if (mapName.StartsWith("sandbox"))
            {
                mapName = "sandbox";
            }

            string navMeshFilename = mapName + "-navmesh.bundle";
            string navMeshPath = Path.Combine(new string[] { WaypointsPlugin.NavMeshFolder, navMeshFilename });
            if (!File.Exists(navMeshPath))
            {
                return;
            }

            var bundle = AssetBundle.LoadFromFile(navMeshPath);
            if (bundle == null)
            {
                Logger.LogError($"Error loading navMeshBundle: {navMeshPath}");
                return;
            }

            var assets = bundle.LoadAllAssets(typeof(NavMeshData));
            if (assets == null || assets.Length == 0)
            {
                Logger.LogError($"Bundle did not contain a NavMeshData asset: {navMeshPath}");
                return;
            }

            // Then inject the new navMeshData, while blowing away the old data
            var navMeshData = assets[0] as NavMeshData;
            if (navMeshData == null)
            {
                Logger.LogError($"Bundle did not contain a NavMeshData asset as first export: {navMeshPath}");
                return;
            }

            NavMesh.RemoveAllNavMeshData();
            NavMesh.AddNavMeshData(navMeshData);

            // Unload the bundle, leaving behind currently in use assets, so we can reload it next map
            bundle.Unload(false);

            Logger.LogDebug($"Injected custom navmesh: {navMeshPath}");
        }
    }
}
