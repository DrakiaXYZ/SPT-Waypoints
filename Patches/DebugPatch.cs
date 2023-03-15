using Aki.Reflection.Patching;
using Comfort.Common;
using DrakiaXYZ.Waypoints.Components;
using DrakiaXYZ.Waypoints.Helpers;
using EFT;
using EFT.Game.Spawning;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace DrakiaXYZ.Waypoints.Patches
{
    public class DebugPatch : ModulePatch
    {
        private static List<GameObject> debugObjects = new List<GameObject>();

        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
        }

        [PatchPrefix]
        public static void PatchPrefix()
        {
            if (WaypointsPlugin.DebugEnabled.Value)
            {
                EnableDebug();
            }
        }

        public static void EnableDebug()
        {
            if (!Singleton<IBotGame>.Instantiated)
            {
                Logger.LogError("EnableDebug triggered before the game was started...");
                return;
            }

            IBotGame botGame = Singleton<IBotGame>.Instance;
            if (botGame == null)
            {
                Logger.LogError("Unable to get reference to botGame");
                return;
            }

            BotControllerClass botController = botGame.BotsController;
            if (botController == null)
            {
                Logger.LogError("Unable to get reference to BotController");
                return;
            }

            BotSpawnerClass botSpawner = botController.BotSpawner;
            if (botSpawner == null)
            {
                Logger.LogError("Unable to get reference to botSpawner");
                return;
            }

            // Draw bot zone information
            FieldInfo spawnZoneField = botSpawner.GetType().GetField("botZone_0", BindingFlags.Instance | BindingFlags.NonPublic);
            if (spawnZoneField != null)
            {
                BotZone[] botZones = (BotZone[])spawnZoneField.GetValue(botSpawner);
                if (botZones != null)
                {
                    Logger.LogDebug($"Found botZones! Count: {botZones.Length}");
                    foreach (BotZone botZone in botZones)
                    {
                        drawBotZone(botZone);
                    }
                }
            }
            else
            {
                Logger.LogError("Failed to find spawnZoneField!");
            }

            // Draw spawn point markers
            SpawnPointMarker[] spawnPointMarkers = UnityEngine.Object.FindObjectsOfType<SpawnPointMarker>();
            if (spawnPointMarkers != null)
            {
                Logger.LogDebug($"Found {spawnPointMarkers.Length} SpawnPointMarkers");
                foreach (SpawnPointMarker spawnPointMarker in spawnPointMarkers)
                {
                    var spawnPoint = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    spawnPoint.GetComponent<Renderer>().material.color = Color.blue;
                    spawnPoint.GetComponent<Collider>().enabled = false;
                    spawnPoint.transform.localScale = new Vector3(0.1f, 1.0f, 0.1f);
                    spawnPoint.transform.position = new Vector3(spawnPointMarker.Position.x, spawnPointMarker.Position.y + 1.0f, spawnPointMarker.Position.z);

                    debugObjects.Add(spawnPoint);
                }
            }

            //// Console log the nav mesh names
            //NavMeshSurface[] navMeshes = GameObject.FindObjectsOfType<NavMeshSurface>();
            //if (navMeshes != null)
            //{
            //    Logger.LogInfo($"{navMeshes.Length} NavMeshes Found");
            //    foreach (NavMeshSurface navMesh in navMeshes)
            //    {
            //        Logger.LogInfo($"    {navMesh.name}");
            //    }
            //}

            //NavMeshDebugComponent.Enable();
        }

        public static void DisableDebug()
        {
            debugObjects.ForEach(UnityEngine.Object.Destroy);
            debugObjects.Clear();
        }

        private static void drawBotZone(BotZone botZone)
        {
            Logger.LogDebug($"Drawing BotZone {botZone.NameZone}");
            Logger.LogDebug($"BushPoints (Green): {botZone.BushPoints.Length}");
            Logger.LogDebug($"CoverPoints (Blue): {botZone.CoverPoints.Length}");
            Logger.LogDebug($"AmbushPoints (Red): {botZone.AmbushPoints.Length}");
            Logger.LogDebug($"PatrolWays: {botZone.PatrolWays.Length}");

            // Bushpoints are green
            foreach (CustomNavigationPoint bushPoint in botZone.BushPoints)
            {
                debugObjects.Add(GameObjectHelper.drawSphere(botZone, bushPoint.Position, 0.5f, Color.green));
            }

            // Coverpoints are blue
            var coverPoints = botZone.GetCoverPoints();
            foreach (CustomNavigationPoint coverPoint in coverPoints)
            {
                debugObjects.Add(GameObjectHelper.drawSphere(botZone, coverPoint.Position, 0.5f, Color.blue));
            }

            // Ambushpoints are red
            var ambushPoints = botZone.GetAmbushPoints();
            foreach (CustomNavigationPoint ambushPoint in ambushPoints)
            {
                debugObjects.Add(GameObjectHelper.drawSphere(botZone, ambushPoint.Position, 0.5f, Color.red));
            }

            // Patrol points are yellow
            var patrolWays = botZone.PatrolWays;
            foreach (PatrolWay patrolWay in patrolWays)
            {
                foreach (PatrolPoint patrolPoint in patrolWay.Points)
                {
                    debugObjects.Add(GameObjectHelper.drawSphere(botZone, patrolPoint.Position, 0.5f, Color.yellow));

                    if (WaypointsPlugin.ShowSubPoints.Value)
                    {
                        foreach (PatrolPoint subPoint in patrolPoint.subPoints)
                        {
                            debugObjects.Add(GameObjectHelper.drawSphere(botZone, subPoint.Position, 0.25f, Color.magenta));
                        }
                    }
                }
            }
        }
    }
}
