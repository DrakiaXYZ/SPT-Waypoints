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
            FindObjectsOfType<Door>().ExecuteForEach(door =>
            {
                // We don't support doors that aren't on the "Interactive" layer
                if (door.gameObject.layer != LayerMaskClass.InteractiveLayer)
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
