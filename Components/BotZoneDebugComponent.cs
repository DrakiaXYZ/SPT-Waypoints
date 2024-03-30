using Aki.Reflection.Utils;
using Comfort.Common;
using DrakiaXYZ.Waypoints.Helpers;
using EFT;
using EFT.Game.Spawning;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DrakiaXYZ.Waypoints.Components
{
    internal class BotZoneDebugComponent : MonoBehaviour, IDisposable
    {
        private static List<UnityEngine.Object> gameObjects = new List<UnityEngine.Object>();

        private List<SpawnPointMarker> spawnPoints = new List<SpawnPointMarker>();
        private List<BotZone> botZones = new List<BotZone>();

        public void Awake()
        {
            Console.WriteLine("BotZoneDebug::Awake");

            // Cache spawn points so we don't constantly need to re-fetch them
            CachePoints(true);

            // Create static game objects
            createSpawnPointObjects();
            createBotZoneObjects();
        }

        public void Dispose()
        {
            Console.WriteLine("BotZoneDebugComponent::Dispose");
            gameObjects.ForEach(Destroy);
            gameObjects.Clear();
            spawnPoints.Clear();
            botZones.Clear();
        }

        private void createSpawnPointObjects()
        {
            // Draw spawn point markers
            if (spawnPoints.Count > 0)
            {
                Console.WriteLine($"Found {spawnPoints.Count} SpawnPointMarkers");
                foreach (SpawnPointMarker spawnPointMarker in spawnPoints)
                {
                    var spawnPoint = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    spawnPoint.GetComponent<Renderer>().material.color = Color.blue;
                    spawnPoint.GetComponent<Collider>().enabled = false;
                    spawnPoint.transform.localScale = new Vector3(0.1f, 1.0f, 0.1f);
                    spawnPoint.transform.position = new Vector3(spawnPointMarker.Position.x, spawnPointMarker.Position.y + 1.0f, spawnPointMarker.Position.z);

                    gameObjects.Add(spawnPoint);
                }
            }
        }

        private void createBotZoneObjects()
        {
            var botGame = Singleton<IBotGame>.Instance;
            var pointsList = botGame.BotsController.CoversData.Points;

            var BushCovers = pointsList.Where(x => x.CoverType == CoverType.Foliage);
            var WallCovers = pointsList.Where(x => x.CoverType == CoverType.Wall);
            var ShootCovers = WallCovers.Where(x => x.PointWithNeighborType == PointWithNeighborType.cover || x.PointWithNeighborType == PointWithNeighborType.both);
            var AmbushCovers = WallCovers.Where(x => x.PointWithNeighborType == PointWithNeighborType.ambush);

            Console.WriteLine($"BushCovers (Green): {BushCovers.Count()}");
            Console.WriteLine($"WallCovers (Blue): {WallCovers.Count()}");
            Console.WriteLine($"ShootCovers (Cyan): {ShootCovers.Count()}");
            Console.WriteLine($"AmbushCovers (Red): {AmbushCovers.Count()}");

            // BushCovers are green
            foreach (var point in BushCovers)
            {
                gameObjects.Add(GameObjectHelper.drawSphere(point.Position, 0.5f, Color.green));
            }

            // WallCovers are blue
            foreach (var point in WallCovers)
            {
                gameObjects.Add(GameObjectHelper.drawSphere(point.Position, 0.5f, Color.blue));
            }

            // ShootCovers are cyan
            foreach (var point in ShootCovers)
            {
                gameObjects.Add(GameObjectHelper.drawSphere(point.Position, 0.5f, Color.cyan));
            }

            // Ambushpoints are red
            foreach (var point in AmbushCovers)
            {
                gameObjects.Add(GameObjectHelper.drawSphere(point.Position, 0.5f, Color.red));
            }

            //// Patrol points are yellow
            //var patrolWays = botZone.PatrolWays;
            //foreach (PatrolWay patrolWay in patrolWays)
            //{
            //    foreach (PatrolPoint patrolPoint in patrolWay.Points)
            //    {
            //        gameObjects.Add(GameObjectHelper.drawSphere(patrolPoint.Position, 0.5f, Color.yellow));

            //        //// Sub-points are purple
            //        //foreach (PatrolPoint subPoint in patrolPoint.subPoints)
            //        //{
            //        //    gameObjects.Add(GameObjectHelper.drawSphere(subPoint.Position, 0.25f, Color.magenta));
            //        //}
            //    }
            //}
        }

        private void CachePoints(bool forced)
        {
            if (forced || spawnPoints.Count == 0)
            {
                spawnPoints = FindObjectsOfType<SpawnPointMarker>().ToList();
            }

            if (forced || botZones.Count == 0)
            {
                botZones = LocationScene.GetAll<BotZone>().ToList();
            }
        }

        public static void Enable()
        {
            if (Singleton<IBotGame>.Instantiated && Settings.DebugEnabled.Value)
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                gameObjects.Add(gameWorld.GetOrAddComponent<BotZoneDebugComponent>());
            }
        }

        public static void Disable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                gameWorld.GetComponent<BotZoneDebugComponent>()?.Dispose();
            }
        }
    }
}
