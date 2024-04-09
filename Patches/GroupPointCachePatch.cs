using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using System.Collections.Generic;
using System.Reflection;

namespace DrakiaXYZ.Waypoints.Patches
{
    /**
     * `CoversData` is static, so isntead of iterating through the 3d array on every bot spawn,
     * iterate through it once on map load and cache the results
     */
    public class GroupPointCachePatch : ModulePatch
    {
        public static List<GroupPoint> CachedGroupPoints = new List<GroupPoint>();

        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
        }

        [PatchPostfix]
        public static void PatchPostfix(GameWorld __instance)
        {
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
                                CachedGroupPoints.Add(groupPoint);
                            }
                        }
                    }
                }
            }

            foreach (GroupPoint groupPoint2 in data.AIManualPointsHolder.ManualPoints)
            {
                CachedGroupPoints.Add(groupPoint2);
            }
        }
    }
}
