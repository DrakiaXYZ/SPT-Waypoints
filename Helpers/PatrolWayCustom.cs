using EFT;

namespace DrakiaXYZ.Waypoints.Helpers
{
    public class PatrolWayCustom : PatrolWay
    {
        // Custom patrol ways will always be suitable
        public override bool Suitable(BotOwner bot, IGetProfileData data)
        {
            return true;
        }
    }
}
