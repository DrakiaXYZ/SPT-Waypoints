using Aki.Reflection.Patching;
using Comfort.Common;
using DrakiaXYZ.Waypoints.Helpers;
using EFT;
using HarmonyLib;
using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

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

        /// <summary>
        /// Make any adjustments to the map we need to make
        /// </summary>
        [PatchPostfix]
        private static void PatchPostfix(BotsController __instance)
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            FixMap(gameWorld);
        }

        private static void InjectNavmesh(GameWorld gameWorld)
        {
            // First we load the asset from the bundle
            string mapName = gameWorld.MainPlayer.Location.ToLower();

            // Standardize Factory
            if (mapName.StartsWith("factory4"))
            {
                mapName = "factory4";
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

        // Some maps need special treatment, to fix bad map data
        private static void FixMap(GameWorld gameWorld)
        {
            string mapName = gameWorld.MainPlayer.Location.ToLower();

            // Factory, Gate 1 door, breached carver is angled wrong, disable it
            if (mapName.StartsWith("factory"))
            {
                var doorLinks = UnityEngine.Object.FindObjectsOfType<NavMeshDoorLink>();

                var gate1DoorLink = doorLinks.Single(x => x.name == "DoorLink_7");
                if (gate1DoorLink != null)
                {
                    gate1DoorLink.Carver_Breached.enabled = false;
                }
            }
            // For Streets, we want to inject a mesh into Chek15, so bots can get inside
            else if (mapName == "tarkovstreets")
            {
                Logger.LogDebug("Injecting custom box colliders to expand Streets bot access");
                GameObject chek15LobbyAddonRamp = new GameObject("chek15LobbyAddonRamp");
                chek15LobbyAddonRamp.layer = LayerMaskClass.LowPolyColliderLayer;
                chek15LobbyAddonRamp.transform.position = new Vector3(126.88f, 2.96f, 229.91f);
                chek15LobbyAddonRamp.transform.localScale = new Vector3(1.0f, 0.1f, 1.0f);
                chek15LobbyAddonRamp.transform.Rotate(new Vector3(0f, 23.65f, 25.36f));
                chek15LobbyAddonRamp.transform.SetParent(gameWorld.transform);
                chek15LobbyAddonRamp.AddComponent<BoxCollider>();

                GameObject chek15BackAddonRamp = new GameObject("Chek15BackAddonRamp");
                chek15BackAddonRamp.layer = LayerMaskClass.LowPolyColliderLayer;
                chek15BackAddonRamp.transform.position = new Vector3(108.31f, 3.32f, 222f);
                chek15BackAddonRamp.transform.localScale = new Vector3(1.0f, 0.1f, 1.0f);
                chek15BackAddonRamp.transform.Rotate(new Vector3(-40f, 0f, 0f));
                chek15BackAddonRamp.transform.SetParent(gameWorld.transform);
                chek15BackAddonRamp.AddComponent<BoxCollider>();
            }
        }
    }
}
