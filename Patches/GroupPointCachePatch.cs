using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using System.Collections.Generic;
using System.Reflection;

namespace DrakiaXYZ.Waypoints.Patches
{
    /**
     * `CoversData` is static, so instead of iterating through the 3d array on every bot spawn,
     * iterate through it once on map load and cache the results
     */
    public class GroupPointCachePatch : ModulePatch
    {
        public static List<CustomNavigationPoint> CachedNavPoints = new List<CustomNavigationPoint>();

        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
        }

        [PatchPostfix]
        public static void PatchPostfix(GameWorld __instance)
        {
            // Clear before we add anything to it
            CachedNavPoints.Clear();

            var botGame = Singleton<IBotGame>.Instance;
            var data = botGame.BotsController.CoversData;

            for (int i = 0; i < data.MaxX; i++)
            {
                for (int j = 0; j < data.MaxY; j++)
                {
                    for (int k = 0; k < data.MaxZ; k++)
                    {
                        NavGraphVoxelSimple navGraphVoxelSimple = data.VoxelesArray[i, j, k];
                        if (navGraphVoxelSimple != null && navGraphVoxelSimple.Points != null)
                        {
                            foreach (GroupPoint groupPoint in navGraphVoxelSimple.Points)
                            {
                                CachedNavPoints.Add(groupPoint.CreateCustomNavigationPoint(0));
                            }
                        }
                    }
                }
            }

            foreach (GroupPoint groupPoint in data.AIManualPointsHolder.ManualPoints)
            {
                CachedNavPoints.Add(groupPoint.CreateCustomNavigationPoint(0));
            }
        }
    }
}
