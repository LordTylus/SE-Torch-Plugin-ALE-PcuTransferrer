using Sandbox.Game.World;
using VRage.Game.ModAPI;

namespace ALE_PcuTransferrer.Utils
{
    class FactionUtils
    {
        public static string GetPlayerFactionTag(long playerId) {

            var faction = MySession.Static.Factions.TryGetPlayerFaction(playerId);

            if (faction == null)
                return "";

            return faction.Tag;
        }

        public static IMyFaction GetIdentityByTag(string tag) {
            return MySession.Static.Factions.TryGetFactionByTag(tag);
        }

        public static IMyFaction GetIdentityById(long factionId) {
            return MySession.Static.Factions.TryGetFactionById(factionId);
        }
    }
}
