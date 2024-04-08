using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace DrakiaXYZ.Waypoints.Patches
{
    internal class DoorLinkPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsController), nameof(BotsController.Init));
        }

        [PatchPostfix]
        public static void PatchPostfix(BotsController __instance)
        {
            var aiDoorsHolder = UnityEngine.Object.FindObjectOfType<AIDoorsHolder>();
            var doorsController = BotDoorsController.CreateOrFind(false);

            int i = 0;

            // Prior to finding door links, add our own doorlinks for locked/breachable doors
            Door[] doors = UnityEngine.Object.FindObjectsOfType<Door>();
            foreach (Door door in doors)
            {
                // We only want locked or breachable doors, or those outside the usual AiCells for the map
                if (!IsValidDoor(door, __instance.CoversData))
                {
                    continue;
                }

                // Find the location of the new doorlink for both open and breached state
                Vector3 hingePos = door.transform.position;
                Vector3 openPos = door.transform.position + (door.GetDoorRotation(door.GetAngle(EDoorState.Open)) * WorldInteractiveObject.GetRotationAxis(door.DoorForward, door.transform));
                Vector3 breachPos = door.transform.position + (door.GetDoorRotation(door.GetAngle(EDoorState.Breaching)) * WorldInteractiveObject.GetRotationAxis(door.DoorForward, door.transform));
                Vector3 shutPos = door.transform.position + (door.GetDoorRotation(door.GetAngle(EDoorState.Shut)) * WorldInteractiveObject.GetRotationAxis(door.DoorForward, door.transform));

                // Create the DoorLink object and setup its properties, create the carvers
                GameObject gameObject = new GameObject($"DoorLink_Custom_{i}");
                NavMeshDoorLink navMeshDoorLink = gameObject.AddComponent<NavMeshDoorLink>();
                navMeshDoorLink.Close1 = hingePos;
                navMeshDoorLink.Open1 = hingePos;
                navMeshDoorLink.Close2_Normal = openPos;
                navMeshDoorLink.Close2_Breach = breachPos;
                navMeshDoorLink.Open2 = shutPos;
                navMeshDoorLink.MidOpen = (navMeshDoorLink.Open1 + navMeshDoorLink.Open2) / 2f;
                navMeshDoorLink.MidClose = (navMeshDoorLink.Close1 + navMeshDoorLink.Close2_Normal) / 2f;

                // Assign it to the BotCellController, same as the other DoorLink objects
                gameObject.transform.SetParent(aiDoorsHolder.transform);
                gameObject.transform.position = door.transform.position;

                // Create the navmesh carvers for when the door is open
                navMeshDoorLink.TryCreateCrave();

                // Setup door state
                navMeshDoorLink.ShallTryInteract = true;
                navMeshDoorLink.Init(__instance);
                navMeshDoorLink.SetDoor(door, true);

                // Add to the AiCellData and BotDoorsController
                AddToCells(__instance.CoversData, door, navMeshDoorLink);
                doorsController._navMeshDoorLinks.Add(navMeshDoorLink);
                i++;
            }

            // Refresh the door for all doorlinks, this will properly set the carving state
            int openDoors = 0;
            int shutDoors = 0;
            int lockedDoors = 0;
            foreach (var doorLink in doorsController._navMeshDoorLinks)
            {
                if (!doorLink.ShallTryInteract)
                {
                    doorLink.ShallTryInteract = true;
                    doorLink.SetDoor(doorLink.Door, true);
                    doorLink.CheckAfterCreatedCarver();
                }

                // Setup the closed carver so we can use it later, enable if the door is locked
                doorLink.Carver_Closed.enabled = true;
                doorLink.Carver_Closed.carving = (doorLink.Door.DoorState == EDoorState.Locked);

                if (doorLink.Door.DoorState == EDoorState.Open) openDoors++;
                if (doorLink.Door.DoorState == EDoorState.Shut) shutDoors++;
                if (doorLink.Door.DoorState == EDoorState.Locked) lockedDoors++;
            }

            Logger.LogInfo($"Open: {openDoors}  Closed: {shutDoors}  Locked: {lockedDoors}");
        }

        private static void AddToCells(AICoversData coversData, Door door, NavMeshDoorLink navMeshDoorLink)
        {
            Vector3 center = door.transform.position;

            int centerX = GetCellX(coversData, center);
            int centerY = GetCellY(coversData, center);
            int centerZ = GetCellZ(coversData, center);

            for (int x = centerX - 1; x <= centerX + 1; x++)
            {
                for (int y = centerY - 1; y <= centerY + 1; y++)
                {
                    for (int z = centerZ - 1; z <= centerZ + 1; z++)
                    {
                        // Make sure our bounds are valid
                        if (x < 0 || z < 0) continue;

                        // Get the cell and validate it has a links list
                        NavGraphVoxelSimple voxel = coversData.GetVoxelSafeByIndexes(x, y, z);
                        if (voxel.DoorLinks == null) voxel.DoorLinks = new List<NavMeshDoorLink>();

                        // Add the doorlink if it doesn't exist
                        if (!voxel.DoorLinks.Contains(navMeshDoorLink))
                        {
                            voxel.DoorLinks.Add(navMeshDoorLink);
                            voxel.DoorLinksIds.Add(navMeshDoorLink.Id);
                        }
                    }
                }
            }
        }

        private static bool IsValidDoor(Door door, AICoversData coversData)
        {
            // Any door outside the cells is valid
            if (IsOutsideCells(coversData, door.transform.position))
            {
                return true;
            }

            // If the door has a NavMeshObstacle child, remove it and consider the door valid
            var obstacle = door.gameObject.GetComponentInChildren<NavMeshObstacle>();
            if (obstacle != null)
            {
                UnityEngine.Object.Destroy(obstacle);
                return true;
            }
            
            // If the door is locked, and either is breachable, or doesn't have an empty key, it's valid
            if (door.InitialDoorState == EDoorState.Locked && (door.CanBeBreached || !string.IsNullOrEmpty(door.KeyId)))
            {
                return true;
            }

            return false;
        }

        private static int GetCellX(AICoversData coversData, Vector3 pos)
        {
            return (int)((float)((int)(pos.x - coversData.MinVoxelesValues.x)) / 10f);
        }

        private static int GetCellY(AICoversData coversData, Vector3 pos)
        {
            return (int)((float)((int)(pos.y - coversData.MinVoxelesValues.y)) / 5f);
        }

        private static int GetCellZ(AICoversData coversData, Vector3 pos)
        {
            return (int)((float)((int)(pos.z - coversData.MinVoxelesValues.z)) / 10f);
        }

        private static bool IsOutsideCells(AICoversData coversData, Vector3 pos)
        {
            int x = GetCellX(coversData, pos);
            int y = GetCellY(coversData, pos);
            int z = GetCellZ(coversData, pos);

            if (x < coversData.MaxX && y < coversData.MaxY && z < coversData.MaxZ)
            {
                return false;
            }

            return true;
        }
    }
}
