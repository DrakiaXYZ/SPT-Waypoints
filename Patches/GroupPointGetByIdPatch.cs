using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;

namespace DrakiaXYZ.Waypoints.Patches
{
    public class GroupPointGetByIdPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GroupPoint).GetMethod(nameof(GroupPoint.GetById));
        }

        [PatchPrefix]
        public static bool PatchPrefix(Dictionary<int, CustomNavigationPoint> ____childs, ref CustomNavigationPoint __result)
        {
            __result = ____childs[0];

            // Skip original
            return false;
        }
    }
}
