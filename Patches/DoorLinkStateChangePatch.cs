using SPT.Reflection.Patching;
using EFT.Interactive;
using HarmonyLib;
using System.Reflection;

namespace DrakiaXYZ.Waypoints.Patches
{
    internal class DoorLinkStateChangePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(NavMeshDoorLink), method => 
                method.GetParameters().Length == 3 && 
                method.GetParameters()[0].ParameterType == typeof(WorldInteractiveObject)
            );
        }

        [PatchPrefix]
        public static void PatchPrefix(NavMeshDoorLink __instance, EDoorState prevstate, EDoorState nextstate)
        {
            if (!__instance.ShallTryInteract) return;

            // Moving away from locked, disable the closed carver
            if (prevstate == EDoorState.Locked)
            {
                __instance.Carver_Closed.carving = false;
            }
            // Moving to locked, enable the closed carver
            else if (nextstate == EDoorState.Locked)
            {
                __instance.Carver_Closed.carving = true;
            }
        }
    }
}
