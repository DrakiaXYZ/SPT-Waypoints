using Aki.Reflection.Patching;
using DrakiaXYZ.Waypoints.Components;
using EFT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DrakiaXYZ.Waypoints.Patches
{
    internal class AICellDataGetCellPatch : ModulePatch
    {
        private static AICell emptyCell;

        protected override MethodBase GetTargetMethod()
        {
            emptyCell = new AICell();
            emptyCell.Links = new NavMeshDoorLink[0];

            return AccessTools.Method(typeof(AICellData), "GetCell");
        }

        [PatchPrefix]
        public static bool PatchPrefix(int i, int j, AICellData __instance, ref AICell __result)
        {
            int offset = i + (j * __instance.MaxIx);
            if (offset < __instance.List.Length)
            {
                __result = __instance.List[offset];
            }
            else 
            {
                __result = emptyCell;
            }

            return false;
        }
    }
}
