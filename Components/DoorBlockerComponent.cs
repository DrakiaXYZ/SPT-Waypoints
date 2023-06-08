using BepInEx.Logging;
using Comfort.Common;
using DrakiaXYZ.Waypoints.Helpers;
using EFT;
using EFT.Interactive;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DrakiaXYZ.Waypoints.Components
{
    // A component that can be attached to a door, that will enable a NavMeshBlocker if the door is locked
    // or otherwise not usable by a bot
    internal class DoorBlockerComponent : MonoBehaviour
    {
        private Door door = null;
        private NavMeshObstacle navMeshObstacle = null;
        private GameObject sphere = null;

        protected static ManualLogSource Logger = null;

        DoorBlockerComponent()
        {
            if (Logger == null)
            {
                Logger = BepInEx.Logging.Logger.CreateLogSource(GetType().Name);
            }
        }

        public void Awake()
        {
            Collider collider = GetComponentInChildren<MeshCollider>();
            if (collider == null)
            {
                GameObjectHelper.drawSphere(gameObject.transform.position, 0.5f, Color.magenta);
                Logger.LogDebug($"Unable to find mesh collider, skipping ({gameObject.transform.position})");
                Destroy(this);
                return;
            }

            // If the door is a door, and it's open or shut, we don't need to worry about it
            door = GetComponent<Door>();
            if (door != null && (door.DoorState == EDoorState.Open || door.DoorState == EDoorState.Shut))
            {
                GameObjectHelper.drawSphere(collider.bounds.center, 0.5f, Color.blue);
                Logger.LogDebug($"Found an open/closed door, skipping");
                Destroy(this);
                return;
            }

            // Make sure the door is tall, otherwise it's probably not a real door
            if (collider.bounds.size.y < 1.5f)
            {
                GameObjectHelper.drawSphere(collider.bounds.center, 0.5f, Color.yellow);
                Logger.LogDebug($"Found a door that's not tall enough, skipping ({collider.bounds.center}) ({collider.bounds.size})");
                Destroy(this);
                return;
            }

            if (door == null || 
                door.DoorState == EDoorState.Locked ||
                !door.Operatable)
            {
                GameObject obstacleObject = new GameObject("ObstacleObject");
                // We attach the obstacle to the parent so we have a more sane position
                navMeshObstacle = obstacleObject.AddComponent<NavMeshObstacle>();

                // We use a small cube, to avoid cutting into the hallway mesh
                navMeshObstacle.size = new Vector3(0.2f, 0.2f, 0.2f);
                navMeshObstacle.carving = true;
                navMeshObstacle.carveOnlyStationary = false;

                // Position the new gameObject
                obstacleObject.transform.SetParent(collider.transform);
                obstacleObject.transform.position = collider.bounds.center;
                obstacleObject.transform.rotation = collider.transform.rotation;
#if DEBUG
                sphere = GameObjectHelper.drawSphere(obstacleObject.transform.position, 0.5f, Color.red);
#endif

                // If the door isn't in a locked state, we don't need to keep tabs on it, so destroy ourselves
                // This only works because we created the NavMeshModifier on the door itself
                if (door == null || door.DoorState != EDoorState.Locked)
                {
                    Destroy(this);
                }
            }
            else
            {
                GameObjectHelper.drawSphere(collider.bounds.center, 0.5f, Color.green);
            }
        }

        public void Update()
        {
            // If the door has been unlocked, delete the blocker, then ourselves
            if (navMeshObstacle != null && door.DoorState != EDoorState.Locked)
            {
                if (sphere != null)
                {
                    Destroy(sphere);
                    sphere = null;
                }

                Destroy(navMeshObstacle);
                navMeshObstacle = null;
                Destroy(this);
            }
        }
    }
}
