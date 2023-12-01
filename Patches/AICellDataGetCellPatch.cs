using Aki.Reflection.Patching;
using HarmonyLib;
using System;
using System.Reflection;

namespace DrakiaXYZ.Waypoints.Patches
{
    internal class AICellDataGetCellPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AICellData), "GetCell");
        }

        [PatchPrefix]
        public static bool PatchPrefix(int i, int j, AICellData __instance, ref AICell __result)
        {
            int offset = i + (j * __instance.MaxIx);
            if (i < __instance.MaxIx && j < __instance.MaxIz && offset < __instance.List.Length)
            {
                __result = __instance.List[offset];
            }
            else 
            {
                if (__instance.List.Length < (__instance.MaxIx * __instance.MaxIz) + 1)
                {
                    Array.Resize(ref __instance.List, __instance.List.Length + 1);

                    AICell emptyCell = new AICell();
                    emptyCell.Links = new NavMeshDoorLink[0];
                    __instance.List[__instance.List.Length - 1] = emptyCell;
                }

                __result = __instance.List[__instance.List.Length - 1];
            }

            return false;
        }
    }
}
