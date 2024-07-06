using SPT.Reflection.Patching;
using EFT;
using EFT.Interactive;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace DrakiaXYZ.Waypoints.Patches
{
    internal class ExfilDoorBlockerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
        }

        [PatchPrefix]
        public static void PatchPrefix()
        {
            // Look for any switch, and then find any collider on the LowPolyCollider layer
            // If found, add a navmesh obstacle that gets disable on state change
            Object.FindObjectsOfType<ExfiltrationDoor>().ExecuteForEach(door =>
            {
                // Skip disabled items, those without subscibees, and ones that are already flagged as open
                if (!door.enabled || door.Subscribee == null || door.OpenStatus.Contains(door.Subscribee.Status)) return;

                // Skip if the game object has a parent ExfiltrationDoor
                if (door.transform.parent != null && door.transform.parent.GetComponentInParent<ExfiltrationDoor>() != null) return;

                // Add navmesh blockers to all the child colliders, and store them so we can disable them later
                List<NavMeshObstacle> navMeshObstacles = new List<NavMeshObstacle>();
                foreach (var collider in door.List_0)
                {
                    NavMeshObstacle navMeshObstacle = collider.gameObject.AddComponent<NavMeshObstacle>();
                    navMeshObstacle.size = collider.bounds.size;
                    navMeshObstacle.carving = true;
                    navMeshObstacles.Add(navMeshObstacle);
                }

                // Subscribe to the state change, and toggle the navmesh based on whether the door is "open"
                door.Subscribee.OnStatusChanged += (point, prevStatus) =>
                {
                    if (door.OpenStatus.Contains(point.Status))
                    {
                        navMeshObstacles.ExecuteForEach(obstacle => obstacle.carving = false);
                    }
                    else
                    {
                        navMeshObstacles.ExecuteForEach(obstacle => obstacle.carving = true);
                    }
                };
            });
        }
    }
}
