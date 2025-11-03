using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace DrakiaXYZ.Waypoints.Patches
{
    internal class FindPathPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotPathFinderClass).GetMethod(nameof(BotPathFinderClass.FindPath));
        }

        [PatchPrefix]
        public static bool PatchPrefix(Vector3 f, Vector3 t, out Vector3[] corners, ref bool __result)
        {
            NavMeshPath navMeshPath = new NavMeshPath();
            if (NavMesh.CalculatePath(f, t, -1, navMeshPath) && navMeshPath.status != NavMeshPathStatus.PathInvalid)
            {
                corners = navMeshPath.corners;
                __result = true;
            }
            else
            {
                corners = null;
                __result = false;
            }

            return false;
        }
    }
}
