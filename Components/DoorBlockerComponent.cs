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
            // Get the base Door component, so we can keep tabs on its open state
            door = GetComponent<Door>();
            if (door == null)
            {
                Destroy(this);
                return;
            }

            Collider collider = GetComponentInChildren<MeshCollider>();
            if (collider == null)
            {
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

                GameObjectHelper.drawSphere(obstacleObject.transform.position, 0.5f, Color.red);

                // If the door isn't in a locked state, we don't need to keep tabs on it, so destroy ourselves
                // This only works because we created the NavMeshModifier on the door itself
                if (door == null || door.DoorState != EDoorState.Locked)
                {
                    Destroy(this);
                }
            }
        }

        public void Update()
        {
            // If the door has been unlocked, delete the blocker, then ourselves
            if (navMeshObstacle != null && door.DoorState != EDoorState.Locked)
            {
                Destroy(navMeshObstacle);
                navMeshObstacle = null;
                Destroy(this);
            }
        }
    }
}
