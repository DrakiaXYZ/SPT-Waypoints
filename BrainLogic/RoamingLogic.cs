using DrakiaXYZ.BigBrain.Brains;
using EFT;
using System;
using UnityEngine;
using UnityEngine.AI;

namespace DrakiaXYZ.Waypoints.BrainLogic
{
    internal class RoamingLogic : CustomLogic
    {
            Vector3? targetPos = null;
            float sprintCheckTime;

            public RoamingLogic(BotOwner bot) : base(bot)
            { }

            public override void Update()
            {
                // Look where you're going
                BotOwner.SetPose(1f);
                BotOwner.Steering.LookToMovingDirection();
                BotOwner.Mover.SetTargetMoveSpeed(1f);

                // Alternate between running and walking
                if (BotOwner.Mover.Sprinting && BotOwner.GetPlayer.Physical.Stamina.NormalValue < 0.3f)
                {
                    BotOwner.Sprint(false);
                }

                // Enough stamina to check? See if we're within our time window
                if (!BotOwner.Mover.Sprinting && BotOwner.GetPlayer.Physical.Stamina.NormalValue > 0.8f)
                {
                    if (sprintCheckTime < Time.time)
                    {
                        sprintCheckTime = Time.time + 5f;

                        // Random chance to sprint
                        int randomChance = UnityEngine.Random.Range(0, 1000);
                        Console.WriteLine($"Stamina: {BotOwner.GetPlayer.Physical.Stamina.NormalValue}  Random: {randomChance}  Chance: {BotOwner.Settings.FileSettings.Patrol.SPRINT_BETWEEN_CACHED_POINTS}");
                        if (randomChance < BotOwner.Settings.FileSettings.Patrol.SPRINT_BETWEEN_CACHED_POINTS)
                        {
                            BotOwner.Sprint(true);
                        }
                    }
                }

                // If we have a target position, and we're already there, clear it
                if (targetPos != null && (Vector3.Distance(targetPos.Value, BotOwner.Position) < 2f))
                {
                    targetPos = null;
                }

                // If we don't have a target position yet, pick one
                int i = 0;
                while (targetPos == null && i < 100)
                {
                    Vector3 randomPos = UnityEngine.Random.insideUnitSphere * 100f;
                    randomPos += BotOwner.Position;
                    if (NavMesh.SamplePosition(randomPos, out var navHit, 100f, NavMesh.AllAreas))
                    {
                        targetPos = navHit.position;
                        BotOwner.GoToPoint(targetPos.Value, true, -1f, false, true, true);
                    }

                    i++;
                }

                if (targetPos == null)
                {
                    Console.WriteLine($"Unable to find a location for {BotOwner.name}");
                }

                BotOwner.DoorOpener.Update();
                gclass271_0.Update(BotOwner);
            }

            private GClass271 gclass271_0 = new GClass271();
    }
}
