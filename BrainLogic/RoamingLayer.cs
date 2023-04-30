using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using System;

namespace DrakiaXYZ.Waypoints.BrainLogic
{
    internal class RoamingLayer : CustomLayer
    {
        protected ManualLogSource Logger;

        public RoamingLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource(this.GetType().Name);
            Logger.LogInfo($"Added roaming to {botOwner.name}");
        }

        public override string GetName()
        {
            return "Roaming";
        }

        public override bool IsActive()
        {
            if (BotOwner.Memory.IsPeace)
            {
                BotOwner.PatrollingData.Pause();
                return true;
            }

            return false;
        }

        public override Action GetNextAction()
        {
            Logger.LogInfo($"Called Roaming GetAction for {BotOwner.name}");
            return new Action(typeof(RoamingLogic), "Why not");
        }

        public override bool IsCurrentActionEnding()
        {
            throw new NotImplementedException();
        }
    }
}
