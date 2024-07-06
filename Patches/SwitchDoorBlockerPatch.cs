using SPT.Reflection.Patching;
using EFT;
using EFT.Interactive;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace DrakiaXYZ.Waypoints.Patches
{
    internal class SwitchDoorBlockerPatch : ModulePatch
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
            var lowPolyColliderLayer = LayerMaskClass.LowPolyColliderLayer;
            Object.FindObjectsOfType<Switch>().ExecuteForEach(switchComponent =>
            {
                // Only run over active, and shut "switch" doors
                if (!switchComponent.enabled || switchComponent.DoorState != EDoorState.Shut) return;

                // Try to find the low poly collider, return if we can't find one
                var colliders = switchComponent.GetComponentsInChildren<Collider>().Where(x => x.gameObject.layer == lowPolyColliderLayer);
                if (colliders == null || colliders.Count() == 0) return;
                var collider = colliders.ElementAt(0); // Just use the first collider we find

                // Add a NavMeshObstacle to the door
                NavMeshObstacle navMeshObstacle = collider.gameObject.AddComponent<NavMeshObstacle>();
                navMeshObstacle.size = collider.bounds.size;
                navMeshObstacle.carving = true;

                // Setup the door to disable the obstacle when it's opened
                switchComponent.OnDoorStateChanged += (door, prevState, nextState) =>
                {
                    // Handle toggling the navmesh carver based on if the door is open or not
                    if (nextState == EDoorState.Open)
                    {
                        navMeshObstacle.carving = false;
                    }
                    else
                    {
                        navMeshObstacle.carving = true;
                    }
                };
            });
        }
    }
}
