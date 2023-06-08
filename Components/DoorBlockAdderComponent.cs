using Comfort.Common;
using EFT;
using EFT.Interactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DrakiaXYZ.Waypoints.Components
{
    internal class DoorBlockAdderComponent : MonoBehaviour
    {
        public void Awake()
        {
            FindObjectsOfType<GameObject>().ExecuteForEach(doorCollider =>
            {
                // We don't support doors that aren't on the "Door" layer
                if (doorCollider.gameObject.layer != LayerMaskClass.DoorLayer)
                {
                    return;
                }

                GameObject door = doorCollider.transform.parent.gameObject;
                // We don't support doors that don't have an "Interactive" parent
                if (door.layer != LayerMaskClass.InteractiveLayer)
                {
                    return;
                }

                door.GetOrAddComponent<DoorBlockerComponent>();
            });
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
}
