using Comfort.Common;
using DrakiaXYZ.Waypoints.Helpers;
using EFT;
using EFT.Game.Spawning;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Gizmos = Popcron.Gizmos;

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

            Gizmos.FrustumCulling = true;

            // Allow the gizmos to also draw on our optics camera
            Gizmos.CameraFilter += cam =>
            {
                return cam.name == "BaseOpticCamera(Clone)";
            };

            // Cache spawn points so we don't constantly need to re-fetch them
            CachePoints(true);

            // Create static game objects
            createSpawnPointObjects();

            // Note: Sometimes this throws an exception between maps, just ignore it
            try
            {
                createBotZoneObjects();
            } 
            catch (Exception)
            {
                // Ignore
            }
        }

        public void Dispose()
        {
            Console.WriteLine("BotZoneDebugComponent::Dispose");
            gameObjects.ForEach(Destroy);
            gameObjects.Clear();
        }

        private void Update()
        {
            if (WaypointsPlugin.DrawGizmos.Value)
            {
                DrawSpawnPointGizmos();
                DrawBotZoneGizmos();
                //DrawPatrolWays();
            }
        }

        private void DrawSpawnPointGizmos()
        {
            foreach (SpawnPointMarker spawnPointMarker in spawnPoints)
            {
                var color = new Color(0.4f, 0.4f, 0.4f, 0.5f);

                Vector3 position = spawnPointMarker.transform.position;
                Vector3 forward = spawnPointMarker.transform.forward;
                Vector3 right = spawnPointMarker.transform.right;
                Vector3 left = spawnPointMarker.transform.right * -1;
                Quaternion rotation = spawnPointMarker.transform.rotation;

                switch (spawnPointMarker.SpawnPoint.Sides)
                {
                    case EPlayerSideMask.None:
                        color = new Color(0f, 0f, 0f, 0.5f);
                        break;
                    case EPlayerSideMask.Usec:
                        color = new Color(0.2f, 0.2f, 0.9f, 0.5f);
                        break;
                    case EPlayerSideMask.Bear:
                        color = new Color(0.9f, 0.2f, 0.2f, 0.5f);
                        break;
                    case EPlayerSideMask.Savage:
                        color = new Color(0.2f, 0.9f, 0.2f, 0.5f);
                        break;
                }

                // Draw a cube outline over the spawn point
                Gizmos.Cube(position + Vector3.up * 1.0f, rotation, new Vector3(0.3f, 1.0f, 0.3f), color);

                // Draw an arrow pointing in the spawn direction
                Gizmos.Line(position + (Vector3.up * 0.5f), position + forward * 2f * 0.3f + (Vector3.up * 0.5f));
                Gizmos.Line(position + forward * 2f * 0.3f + (Vector3.up * 0.5f), position + (forward * 1.5f * 0.3f + left * 0.5f * 0.3f) + (Vector3.up * 0.5f));
                Gizmos.Line(position + forward * 2f * 0.3f + (Vector3.up * 0.5f), position + (forward * 1.5f * 0.3f + right * 0.5f * 0.3f) + (Vector3.up * 0.5f));
            }
        }

        private void DrawBotZoneGizmos()
        {
            foreach (BotZone botZone in botZones)
            {
                foreach (BoxCollider boxCollider in botZone.GetAllBounds(false))
                {
                    Vector3 size = boxCollider.size;
                    Vector3 localScale = boxCollider.transform.localScale;
                    Vector3 boxSize = new Vector3(size.x * localScale.x, size.y * localScale.y, size.z * localScale.z);
                    Gizmos.Cube(boxCollider.transform.position, boxCollider.transform.rotation, boxSize, Color.yellow);
                }
            }
        }

        private void DrawPatrolWays()
        {
            foreach (BotZone botZone in botZones)
            {
                BotZonePatrolData patrolData = botZone.ZonePatrolData;
                foreach (WayPatrolData wayPatrolData in patrolData.WaysAsList)
                {
                    foreach (WayPatrolPoints path in wayPatrolData.Paths)
                    {
                        Vector3 b = Vector3.up * 0.1f;
                        Color color = path.IsAvailable ? (path.CanRun ? Color.green : Color.blue) : Color.red;
                        for (int i = 0; i < path.WayPoints.Length - 1; i++)
                        {
                            Vector3 a = path.WayPoints[i];
                            Vector3 a2 = path.WayPoints[i + 1];
                            Gizmos.Line(a + b, a2 + b, color);
                        }
                    }
                }
            }
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
            foreach (BotZone botZone in botZones)
            {
                Console.WriteLine($"Drawing BotZone {botZone.NameZone}");
                Console.WriteLine($"BushPoints (Green): {botZone.BushPoints.Length}");
                Console.WriteLine($"CoverPoints (Blue): {botZone.CoverPoints.Length}");
                Console.WriteLine($"AmbushPoints (Red): {botZone.AmbushPoints.Length}");
                Console.WriteLine($"PatrolWays: {botZone.PatrolWays.Length}");
                foreach (PatrolWay patrol in botZone.PatrolWays)
                {
                    Console.WriteLine($"    {patrol.name}");
                }

                // Bushpoints are green
                foreach (CustomNavigationPoint bushPoint in botZone.BushPoints)
                {
                    gameObjects.Add(GameObjectHelper.drawSphere(botZone, bushPoint.Position, 0.5f, Color.green));
                }

                // Coverpoints are blue
                var coverPoints = botZone.GetCoverPoints();
                foreach (CustomNavigationPoint coverPoint in coverPoints)
                {
                    gameObjects.Add(GameObjectHelper.drawSphere(botZone, coverPoint.Position, 0.5f, Color.blue));
                }

                // Ambushpoints are red
                var ambushPoints = botZone.GetAmbushPoints();
                foreach (CustomNavigationPoint ambushPoint in ambushPoints)
                {
                    gameObjects.Add(GameObjectHelper.drawSphere(botZone, ambushPoint.Position, 0.5f, Color.red));
                }

                // Patrol points are yellow
                var patrolWays = botZone.PatrolWays;
                foreach (PatrolWay patrolWay in patrolWays)
                {
                    foreach (PatrolPoint patrolPoint in patrolWay.Points)
                    {
                        gameObjects.Add(GameObjectHelper.drawSphere(botZone, patrolPoint.Position, 0.5f, Color.yellow));

                        // Sub-points are purple
                        foreach (PatrolPoint subPoint in patrolPoint.subPoints)
                        {
                            gameObjects.Add(GameObjectHelper.drawSphere(botZone, subPoint.Position, 0.25f, Color.magenta));
                        }
                    }
                }
            }
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
            if (Singleton<IBotGame>.Instantiated && WaypointsPlugin.DebugEnabled.Value)
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
