using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DrakiaXYZ.Waypoints.Patches
{
    public class WaypointPatch : ModulePatch
    {
        private static int customWaypointCount = 0;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotControllerClass).GetMethod(nameof(BotControllerClass.Init));
        }

        /// <summary>
        /// 
        /// </summary>
        [PatchPrefix]
        private static void PatchPrefix(BotControllerClass __instance, IBotGame botGame, IBotCreator botCreator, BotZone[] botZones, ISpawnSystem spawnSystem, BotLocationModifier botLocationModifier, bool botEnable, bool freeForAll, bool enableWaveControl, bool online, bool haveSectants, string openZones)
        {
            if (botZones != null)
            {
                string mapName = "unknown";
                var gameWorld = Singleton<GameWorld>.Instance;
                if (gameWorld != null)
                {
                    mapName = gameWorld.MainPlayer.Location.ToLower();
                }

                // Inject our loaded patrols
                foreach (BotZone botZone in botZones)
                {
                    Dictionary<string, CustomPatrolWay> customPatrols = CustomWaypointLoader.Instance.getMapZonePatrols(mapName, botZone.NameZone);
                    if (customPatrols != null)
                    {
                        Logger.LogInfo($"Found custom patrols for {mapName} / {botZone.NameZone}");
                        foreach (string patrolName in customPatrols.Keys)
                        {
                            // If the map already has this patrol, update its values
                            PatrolWay mapPatrol = botZone.PatrolWays.Single(p => p.name == patrolName);
                            if (mapPatrol != null)
                            {
                                AddPatrol(mapPatrol, customPatrols[patrolName]);
                            }
                            // Otherwise, add a full new patrol
                            else
                            {
                                CreatePatrol(botZone, patrolName, customPatrols[patrolName]);
                            }
                        }
                    }
                }

                Logger.LogInfo($"Loaded {customWaypointCount} custom waypoints!");

                // If debug is enabled, dump the waypoint data
                if (WaypointsPlugin.DebugEnabled.Value)
                {
                    // If we haven't written out the Waypoints for this map yet, write them out now
                    string exportFile = $"BepInEx/plugins/DrakiaXYZ-Waypoints/{mapName}.json";
                    if (!File.Exists(exportFile))
                    {
                        ExportWaypoints(exportFile, botZones);
                    }
                }
            }
        }

        private static void AddPatrol(PatrolWay mapPatrol, CustomPatrolWay customPatrol)
        {
            mapPatrol.BlockRoles = (WildSpawnType?)customPatrol.blockRoles ?? mapPatrol.BlockRoles;
            mapPatrol.MaxPersons = customPatrol.maxPersons ?? mapPatrol.MaxPersons;
            mapPatrol.PatrolType = customPatrol.patrolType ?? mapPatrol.PatrolType;
            mapPatrol.Points.AddRange(processWaypointsToPatrolPoints(customPatrol.waypoints));
        }

        private static List<PatrolPoint> processWaypointsToPatrolPoints(List<CustomWaypoint> waypoints)
        {
            List<PatrolPoint> patrolPoints = new List<PatrolPoint>();
            foreach (CustomWaypoint waypoint in waypoints)
            {
                Logger.LogDebug("Injecting custom PatrolPoint at " + waypoint.position.x + ", " + waypoint.position.y + ", " + waypoint.position.z);
                var newPatrolPointObject = new GameObject("CustomWaypoint_" + (customWaypointCount++));
                newPatrolPointObject.AddComponent<PatrolPoint>();
                var newPatrolPoint = newPatrolPointObject.GetComponent<PatrolPoint>();

                newPatrolPoint.Id = (new System.Random()).Next();
                newPatrolPoint.transform.position = new Vector3(waypoint.position.x, waypoint.position.y, waypoint.position.z);
                newPatrolPoint.CanUseByBoss = waypoint.canUseByBoss;
                newPatrolPoint.PatrolPointType = waypoint.patrolPointType;
                newPatrolPoint.ShallSit = waypoint.shallSit;
                newPatrolPoint.PointWithLookSides = null;
                newPatrolPoint.SubManual = false;
                newPatrolPoint.subPoints = processWaypointsToPatrolPoints(waypoint.waypoints);
                patrolPoints.Add(newPatrolPoint);
            }

            return patrolPoints;
        }

        private static void CreatePatrol(BotZone botZone, string patrolName, CustomPatrolWay customPatrol)
        {
            Logger.LogInfo($"Creating custom patrol {patrolName} in {botZone.NameZone}");
            // Validate some data
            if (customPatrol.blockRoles == null)
            {
                Logger.LogError("Invalid custom Patrol, blockRoles is null");
                return;
            }
            if (customPatrol.maxPersons == null)
            {
                Logger.LogError("Invalid custom Patrol, maxPersons is null");
                return;
            }
            if (customPatrol.patrolType == null)
            {
                Logger.LogError("Invalid custom Patrol, patrolTypes is null");
                return;
            }

            // Create the Patrol game object
            var mapPatrolObject = new GameObject(patrolName);
            mapPatrolObject.AddComponent<PatrolWay>();
            var mapPatrol = mapPatrolObject.GetComponent<PatrolWay>();

            // Add the waypoints to the Patrol object
            AddPatrol(mapPatrol, customPatrol);

            // Add the patrol to our botZone
            botZone.PatrolWays.Append(mapPatrol);
        }

        static void ExportWaypoints(string exportFile, BotZone[] botZones)
        {
            Dictionary<String, Dictionary<String, CustomPatrolWay>> botZonePatrols = new Dictionary<string, Dictionary<String, CustomPatrolWay>>();

            foreach (BotZone botZone in botZones)
            {
                Dictionary<String, CustomPatrolWay> customPatrolWays = new Dictionary<string, CustomPatrolWay>();
                foreach (PatrolWay patrolWay in botZone.PatrolWays)
                {
                    CustomPatrolWay customPatrolWay = new CustomPatrolWay();
                    customPatrolWay.blockRoles = patrolWay.BlockRoles.GetInt();
                    customPatrolWay.maxPersons = patrolWay.MaxPersons;
                    customPatrolWay.patrolType = patrolWay.PatrolType;
                    customPatrolWay.name = patrolWay.name;
                    customPatrolWay.waypoints = CreateCustomWaypoints(patrolWay.Points);

                    customPatrolWays.Add(patrolWay.name, customPatrolWay);
                }

                botZonePatrols.Add(botZone.NameZone, customPatrolWays);
            }

            string jsonString = JsonConvert.SerializeObject(botZonePatrols, Formatting.Indented);
            if (File.Exists(exportFile))
            {
                File.Delete(exportFile);
            }
            File.Create(exportFile).Dispose();
            StreamWriter streamWriter = new StreamWriter(exportFile);
            streamWriter.Write(jsonString);
            streamWriter.Flush();
            streamWriter.Close();
        }

        static List<CustomWaypoint> CreateCustomWaypoints(List<PatrolPoint> patrolPoints)
        {
            List<CustomWaypoint> customWaypoints = new List<CustomWaypoint>();
            if (patrolPoints == null)
            {
                Logger.LogInfo("patrolPoints is null, skipping");
                return customWaypoints;
            }

            foreach (PatrolPoint patrolPoint in patrolPoints)
            {
                CustomWaypoint customWaypoint = new CustomWaypoint();
                customWaypoint.canUseByBoss = patrolPoint.CanUseByBoss;
                customWaypoint.patrolPointType = patrolPoint.PatrolPointType;
                customWaypoint.position = patrolPoint.Position;
                customWaypoint.shallSit = patrolPoint.ShallSit;
                customWaypoint.waypoints = CreateCustomWaypoints(patrolPoint.subPoints);

                customWaypoints.Add(customWaypoint);
            }

            return customWaypoints;
        }
    }



    //public class FindNextPointPatch : ModulePatch
    //{
    //    protected override MethodBase GetTargetMethod()
    //    {
    //        return typeof(GClass479).GetMethod(nameof(GClass479.FindNextPoint));
    //    }

    //    [PatchPostfix]
    //    public static void PatchPostfix(GClass479 __instance, ref BotOwner ___botOwner_0, ref GClass492 __result, bool withSetting, bool withoutNext, int minSubTargets = -1, bool canCut = true, GDelegate1 pointFilter = null)
    //    {
    //        Logger.LogInfo("FindNextPoint called");
    //        if (withSetting)
    //        {
    //            ___botOwner_0.PatrollingData.PointControl.SetTarget(__result, -1);
    //        }
    //    }
    //}



    //public class IsComePatch : ModulePatch
    //{
    //    protected override MethodBase GetTargetMethod()
    //    {
    //        return typeof(GClass423).GetMethod(nameof(GClass423.IsCome));
    //    }

    //    [PatchPostfix]
    //    public static void PatchPostfix(GClass423 __instance, ref bool __result, BotOwner bot, Vector3? curTrg, bool extraTarget, float dist)
    //    {
    //        Logger.LogInfo("IsCome called, result: " + (__result ? "true" : "false") + " dist: " + dist);
    //    }
    //}
}
