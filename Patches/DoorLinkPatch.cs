using Aki.Reflection.Patching;
using EFT.Interactive;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DrakiaXYZ.Waypoints.Patches
{
    internal class DoorLinkPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotCellController), "FindDoorLinks");
        }

        [PatchPrefix]
        public static void PatchPrefix(BotCellController __instance)
        {
            int i = 0;

            // Prior to finding door links, add our own doorlinks for locked/breachable doors
            Door[] doors = UnityEngine.Object.FindObjectsOfType<Door>();
            foreach (Door door in doors)
            {
                // We only want locked or breachable doors, or those outside the usual AiCells for the map
                if (!IsValidDoor(door, __instance))
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
                gameObject.transform.SetParent(__instance.gameObject.transform);
                gameObject.transform.position = door.transform.position;

                // Create the navmesh carvers for when the door is open
                navMeshDoorLink.TryCreateCrave();

                // Add to the AiCellData. NOTE: Will need to redo this for 3.8.0, yay
                AddToCells(__instance, door, navMeshDoorLink);
                i++;
            }
        }

        private static void AddToCells(BotCellController controller, Door door, NavMeshDoorLink navMeshDoorLink)
        {
            Vector3 center = door.transform.position;

            int centerX = GetCellX(controller, center);
            int centerZ = GetCellZ(controller, center);

            for (int i = centerX - 1; i <= centerX + 1; i++)
            {
                for (int j = centerZ - 1; j <= centerZ + 1; j++)
                {
                    // Make sure our bounds are valid
                    if (i < 0 || j < 0) continue;

                    // Get the cell and validate it has a links list
                    AICell cell = controller.Data.GetCell(i, j);
                    if (cell.Links == null) cell.Links = new NavMeshDoorLink[0];
                    if (cell.Links.Contains(navMeshDoorLink)) continue;

                    // Resizing an array is probably slow, but we're only doing this on match start, so should be fine
                    Array.Resize(ref cell.Links, cell.Links.Length + 1);
                    cell.Links[cell.Links.Length - 1] = navMeshDoorLink;
                }
            }
        }

        private static bool IsValidDoor(Door door, BotCellController controller)
        {
            // Any door outside the cells is valid
            if (IsOutsideCells(controller, door.transform.position))
            {
                return true;
            }
            
            // If the door is locked, and either is breachable, or doesn't have an empty key, it's valid
            if (door.InitialDoorState == EDoorState.Locked && (door.CanBeBreached || !string.IsNullOrEmpty(door.KeyId)))
            {
                return true;
            }

            return false;
        }

        private static int GetCellX(BotCellController controller, Vector3 pos)
        {
            return GetCellPos(controller, pos.x, controller.Data.StartX);
        }

        private static int GetCellZ(BotCellController controller, Vector3 pos)
        {
            return GetCellPos(controller, pos.z, controller.Data.StartZ);
        }

        private static bool IsOutsideCells(BotCellController controller, Vector3 pos)
        {
            int x = GetCellX(controller, pos);
            int z = GetCellZ(controller, pos);

            if (x < controller.Data.MaxIx && z < controller.Data.MaxIz)
            {
                return false;
            }

            return true;
        }

        private static int GetCellPos(BotCellController controller, float pos, float startCoef)
        {
            return (int)((pos - startCoef) / controller.Data.CellSize);
        }
    }
}
