using BepInEx.Logging;
using Comfort.Common;
using DrakiaXYZ.Waypoints.Helpers;
using EFT;
using EFT.Interactive;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DrakiaXYZ.Waypoints.Components
{
    internal class DoorBlockAdderComponent : MonoBehaviour
    {
        List<DoorContainer> doorList = new List<DoorContainer>();
        float nextUpdate = 0f;
        protected ManualLogSource Logger = null;

        public void Awake()
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource(GetType().Name);

            FindObjectsOfType<MeshCollider>().ExecuteForEach(meshCollider =>
            {
                // We don't support doors that aren't on the "Door" layer
                if (meshCollider.gameObject.layer != LayerMaskClass.DoorLayer)
                {
                    return;
                }

                // We don't support doors that don't have an "Interactive" parent
                GameObject doorObject = meshCollider.transform.parent.gameObject;
                WorldInteractiveObject door = doorObject.GetComponent<WorldInteractiveObject>();

                // If we don't have a door object, and the layer isn't interactive, skip
                // Note: We have to do a door null check here because Factory has some non-interactive doors that bots can use...
                if (door == null && doorObject.layer != LayerMaskClass.InteractiveLayer)
                {
                    return;
                }

                // If the door is an interactive object, and it's open or shut, we don't need to worry about it
                if (door != null && (door.DoorState == EDoorState.Open || door.DoorState == EDoorState.Shut))
                {
                    drawDebugSphere(meshCollider.bounds.center, 0.5f, Color.blue);
                    //Logger.LogDebug($"Found an open/closed door, skipping");
                    return;
                }

                // Make sure the door is tall, otherwise it's probably not a real door
                if (meshCollider.bounds.size.y < 1.5f)
                {
                    drawDebugSphere(meshCollider.bounds.center, 0.5f, Color.yellow);
                    //Logger.LogDebug($"Found a door that's not tall enough, skipping ({meshCollider.bounds.center}) ({meshCollider.bounds.size})");
                    return;
                }

                if (door == null ||
                    door.DoorState == EDoorState.Locked ||
                    !door.Operatable)
                {
                    GameObject obstacleObject = new GameObject("ObstacleObject");
                    NavMeshObstacle navMeshObstacle = obstacleObject.AddComponent<NavMeshObstacle>();

                    // We use a small cube, to avoid cutting into the hallway mesh
                    navMeshObstacle.size = new Vector3(0.2f, 0.2f, 0.2f);
                    navMeshObstacle.carving = true;
                    navMeshObstacle.carveOnlyStationary = false;

                    // Position the new gameObject
                    obstacleObject.transform.SetParent(meshCollider.transform);
                    obstacleObject.transform.position = meshCollider.bounds.center;
                    obstacleObject.transform.rotation = meshCollider.transform.rotation;

                    // If the door was locked, we want to keep track of it to remove the blocker when it's unlocked
                    if (door != null && door.DoorState == EDoorState.Locked)
                    {
                        DoorContainer doorContainer = new DoorContainer();
                        doorContainer.door = door;
                        doorContainer.meshCollider = meshCollider;
                        doorContainer.navMeshObstacle = navMeshObstacle;
                        doorContainer.sphere = drawDebugSphere(obstacleObject.transform.position, 0.5f, Color.red);
                        doorList.Add(doorContainer);
                    }
                }
            });
        }

        public void Update()
        {
            if (Time.time > nextUpdate)
            {
                for (int i = doorList.Count - 1; i >= 0; i--)
                {
                    DoorContainer doorContainer = doorList[i];

                    // If the door has been unlocked, delete the blocker, then ourselves
                    if (doorContainer.door.DoorState != EDoorState.Locked)
                    {
                        if (doorContainer.sphere != null)
                        {
                            Destroy(doorContainer.sphere);
                        }

                        Destroy(doorContainer.navMeshObstacle);
                        doorList.RemoveAt(i);
                    }
                }

                nextUpdate = Time.time + 0.5f;
            }
        }

        private GameObject drawDebugSphere(Vector3 position, float size, Color color)
        {
#if DEBUG
            return GameObjectHelper.drawSphere(position, size, color);
#else
            return null;
#endif
        }

        public static void Enable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                gameWorld.GetOrAddComponent<DoorBlockAdderComponent>();
            }
        }
    }

    internal struct DoorContainer
    {
        public WorldInteractiveObject door;
        public MeshCollider meshCollider;
        public NavMeshObstacle navMeshObstacle;
        public GameObject sphere;
    }
}
