using Aki.Reflection.Patching;
using EFT;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace DrakiaXYZ.Waypoints.Patches
{
    /**
     * BSG iterates through the whole `VoxelesArray` (3d array) on every bot activate, there's no reason to do this because it's
     * static data... So we instead cache the full list in `GroupPointCachePatch` and then iterate through it here
     */
    public class BotVoxelesPersonalActivatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotVoxelesPersonalData).GetMethod(nameof(BotVoxelesPersonalData.Activate));
        }

        [PatchPrefix]
        public static bool PatchPrefix(AICoversData data, BotOwner ___botOwner_0, ref AICoversData ____data, ref List<CustomNavigationPoint> ____allPoints, out Stopwatch __state)
        {
            __state = new Stopwatch();
            __state.Start();

            Logger.LogInfo("Loading voxel data from cache");
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            ____data = data;
            int id = ___botOwner_0.Id;

            foreach (var groupPoint in GroupPointCachePatch.CachedGroupPoints)
            {
                ____allPoints.Add(groupPoint.CreateCustomNavigationPoint(id));
            }

            stopwatch.Stop();
            Logger.LogInfo($"Voxel Cache Activate took {stopwatch.ElapsedMilliseconds}ms");

            return false;
        }
    }
}
