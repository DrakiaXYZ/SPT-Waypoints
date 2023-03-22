using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using Comfort.Common;
using DrakiaXYZ.Waypoints.Helpers;
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
                            AddOrUpdatePatrol(botZone, customPatrols[patrolName]);
                        }
                    }
                }

                Logger.LogInfo($"Loaded {customWaypointCount} custom waypoints!");

                // If enabled, dump the waypoint data
                if (Settings.ExportMapPoints.Value)
                {
                    // If we haven't written out the Waypoints for this map yet, write them out now
                    Directory.CreateDirectory(WaypointsPlugin.PointsFolder);
                    string exportFile = $"{WaypointsPlugin.PointsFolder}\\{mapName}.json";
                    if (!File.Exists(exportFile))
                    {
                        ExportWaypoints(exportFile, botZones);
                    }
                }
            }
        }

        public static void AddOrUpdatePatrol(BotZone botZone, CustomPatrolWay customPatrol)
        {
            // If the map already has this patrol, update its values
            PatrolWay mapPatrol = botZone.PatrolWays.SingleOrDefault(p => p.name == customPatrol.name);
            if (mapPatrol != null)
            {
                Console.WriteLine($"PatrolWay {customPatrol.name} exists, updating");
                UpdatePatrol(mapPatrol, customPatrol);
            }
            // Otherwise, add a full new patrol
            else
            {
                Console.WriteLine($"PatrolWay {customPatrol.name} doesn't exist, creating");
                AddPatrol(botZone, customPatrol);
            }
        }

        private static void UpdatePatrol(PatrolWay mapPatrol, CustomPatrolWay customPatrol)
        {
            mapPatrol.BlockRoles = (WildSpawnType?)customPatrol.blockRoles ?? mapPatrol.BlockRoles;
            mapPatrol.MaxPersons = customPatrol.maxPersons ?? mapPatrol.MaxPersons;
            mapPatrol.PatrolType = customPatrol.patrolType ?? mapPatrol.PatrolType;

            // Exclude any points that already exist in the map PatrolWay
            var customWaypoints = customPatrol.waypoints.Where(
                p => (mapPatrol.Points.Where(w => w.position == p.position).ToList().Count == 0)
            ).ToList();

            if (customWaypoints.Count > 0)
            {
                mapPatrol.Points.AddRange(processWaypointsToPatrolPoints(customWaypoints));
            }
        }

        private static List<PatrolPoint> processWaypointsToPatrolPoints(List<CustomWaypoint> waypoints)
        {
            List<PatrolPoint> patrolPoints = new List<PatrolPoint>();
            if (waypoints == null)
            {
                return patrolPoints;
            }

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

        private static void AddPatrol(BotZone botZone, CustomPatrolWay customPatrol)
        {
            Logger.LogInfo($"Creating custom patrol {customPatrol.name} in {botZone.NameZone}");
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
            var mapPatrolObject = new GameObject(customPatrol.name);
            mapPatrolObject.AddComponent<PatrolWay>();
            var mapPatrol = mapPatrolObject.GetComponent<PatrolWay>();

            // Add the waypoints to the Patrol object
            UpdatePatrol(mapPatrol, customPatrol);

            // Add the patrol to our botZone
            botZone.PatrolWays = botZone.PatrolWays.Append(mapPatrol).ToArray();
        }

        static void ExportWaypoints(string exportFile, BotZone[] botZones)
        {
            ExportModel exportModel = new ExportModel();

            foreach (BotZone botZone in botZones)
            {
                exportModel.zones.Add(botZone.name, new ExportZoneModel());

                List<CustomPatrolWay> customPatrolWays = new List<CustomPatrolWay>();
                foreach (PatrolWay patrolWay in botZone.PatrolWays)
                {
                    CustomPatrolWay customPatrolWay = new CustomPatrolWay();
                    customPatrolWay.blockRoles = patrolWay.BlockRoles.GetInt();
                    customPatrolWay.maxPersons = patrolWay.MaxPersons;
                    customPatrolWay.patrolType = patrolWay.PatrolType;
                    customPatrolWay.name = patrolWay.name;
                    customPatrolWay.waypoints = CreateCustomWaypoints(patrolWay.Points);

                    customPatrolWays.Add(customPatrolWay);
                }

                exportModel.zones[botZone.name].patrols = customPatrolWays;

                exportModel.zones[botZone.name].coverPoints = botZone.CoverPoints.Select(p => customNavPointToExportNavPoint(p)).ToList();
                exportModel.zones[botZone.name].ambushPoints = botZone.AmbushPoints.Select(p => customNavPointToExportNavPoint(p)).ToList();
            }

            string jsonString = JsonConvert.SerializeObject(exportModel, Formatting.Indented);
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

        static ExportNavigationPoint customNavPointToExportNavPoint(CustomNavigationPoint customNavPoint)
        {
            ExportNavigationPoint exportNavPoint = new ExportNavigationPoint();
            exportNavPoint.AltPosition = customNavPoint.AltPosition;
            exportNavPoint.HaveAltPosition = customNavPoint.HaveAltPosition;
            exportNavPoint.BasePosition = customNavPoint.BasePosition;
            exportNavPoint.ToWallVector = customNavPoint.ToWallVector;
            exportNavPoint.FirePosition = customNavPoint.FirePosition;
            exportNavPoint.TiltType = customNavPoint.TiltType.GetInt();
            exportNavPoint.CoverLevel = customNavPoint.CoverLevel.GetInt();
            exportNavPoint.AlwaysGood = customNavPoint.AlwaysGood;
            exportNavPoint.BordersLightHave = customNavPoint.BordersLightHave;
            exportNavPoint.LeftBorderLight = customNavPoint.LeftBorderLight;
            exportNavPoint.RightBorderLight = customNavPoint.RightBorderLight;
            exportNavPoint.CanLookLeft = customNavPoint.CanLookLeft;
            exportNavPoint.CanLookRight = customNavPoint.CanLookRight;
            exportNavPoint.HideLevel = customNavPoint.HideLevel;

            return exportNavPoint;
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
    //    public static void PatchPostfix(GClass479 __instance, ref BotOwner ___botOwner_0, ref GClass495 __result, bool withSetting, bool withoutNext, int minSubTargets = -1, bool canCut = true, GDelegate1 pointFilter = null)
    //    {
    //        Logger.LogInfo("FindNextPoint called");
    //        if (withSetting)
    //        {
    //            ___botOwner_0.PatrollingData.PointControl.SetTarget(__result, -1);
    //        }
    //    }
    //}



    public class IsComePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass423).GetMethod(nameof(GClass423.IsCome));
        }

        [PatchPostfix]
        public static void PatchPostfix(GClass423 __instance, ref bool __result, BotOwner bot, Vector3? curTrg, bool extraTarget, float dist)
        {
            Logger.LogInfo($"IsCome {bot.name}  result: {__result} dist: {dist}");
        }
    }

    public class PatrollingDataManualUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass428).GetMethod(nameof(GClass428.ManualUpdate));
        }

        [PatchPrefix]
        public static void PatchPrefix(GClass428 __instance, bool canLookAround, float ___float_5, BotOwner ___botOwner_0)
        {
            Logger.LogDebug($"ManualUpdate {___botOwner_0.name} Status: {__instance.Status}  Type: {__instance.CurPatrolPoint?.TargetPoint?.PatrolWay?.PatrolType}");
            Logger.LogDebug($"  float_5: {___float_5}  Time: {Time.time}  ComeToPointTime: {__instance.ComeToPointTime}");
        }
    }

    public class GClass479FindNextPointPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass479).GetMethod(nameof(GClass479.FindNextPoint));
        }

        [PatchPostfix]
        public static void PatchPostfix(GClass479 __instance, BotOwner ___botOwner_0, GClass495 __result)
        {
            Logger.LogDebug($"FindNextPoint {___botOwner_0.name}  {___botOwner_0.Profile.Nickname}  {___botOwner_0.Profile.Info.Settings.Role} Position: {__result.Position} Time: {Time.time}");
        }
    }

    public class PatrollingDataPointChooserPatch : ModulePatch
    {
        private static Type _patrollingDataType;
        private static WildSpawnType _bear;
        private static WildSpawnType _usec;

        static PatrollingDataPointChooserPatch()
        {
            string searchMethodName = "GetPointChooser";
            _patrollingDataType = PatchConstants.EftTypes.Single(x => x.GetMethod(searchMethodName) != null);
            _bear = (WildSpawnType)Aki.PrePatch.AkiBotsPrePatcher.sptBearValue;
            _usec = (WildSpawnType)Aki.PrePatch.AkiBotsPrePatcher.sptUsecValue;
        }

        protected override MethodBase GetTargetMethod()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            string desiredMethodName = "ManualUpdate";
            var desiredMethod = _patrollingDataType.GetMethod(desiredMethodName, flags);

            Logger.LogDebug($"{this.GetType().Name} Type: {_patrollingDataType?.Name}");
            Logger.LogDebug($"{this.GetType().Name} Method: {desiredMethod?.Name}");

            return desiredMethod;
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ___botOwner_0, GClass428 __instance, float ___float_5)
        {
            if (___float_5 > Time.time)
            {
                return true;
            }

            if (__instance.Status == PatrolStatus.stay)
            {
                if (___botOwner_0.Profile.Info.Settings.Role == _bear || ___botOwner_0.Profile.Info.Settings.Role == _usec)
                {
                    Logger.LogDebug($"ManualUpdate {___botOwner_0.name}  {___botOwner_0.Profile.Nickname}  {___botOwner_0.Profile.Info.Settings.Role} Status: {__instance.Status} Time: {Time.time}");
                }
            }

            return true;
        }
    }
}
