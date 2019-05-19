using Sandbox.Game.World;
using VRage.Game.ModAPI;

namespace ALE_PcuTransferrer.Utils
{
    class FactionUtils
    {

        public static IMyFaction GetIdentityByTag(string tag) {
            return MySession.Static.Factions.TryGetFactionByTag(tag);
        }

        public static IMyFaction GetIdentityById(long factionId) {
            return MySession.Static.Factions.TryGetFactionById(factionId);
        }
    }
}
