using Sandbox.Game.World;

namespace ALE_PcuTransferrer.Utils
{
    class PlayerUtils
    {

        public static string GetPlayerNameById(long playerId)
        {

            foreach (var identity in MySession.Static.Players.GetAllIdentities())
                if (identity.IdentityId == playerId)
                    return identity.DisplayName;

            return "Nobody";
        }

        public static long GetPlayerIdByName(string playerName)
        {

            foreach (var identity in MySession.Static.Players.GetAllIdentities())
                if (identity.DisplayName == playerName)
                    return identity.IdentityId;

            return 0;
        }

        public static MyPlayer GetPlayerByNameOrId(string nameOrPlayerId)
        {

            if (!long.TryParse(nameOrPlayerId, out long id))
            {
                foreach (var identity in MySession.Static.Players.GetAllIdentities())
                {
                    if (identity.DisplayName == nameOrPlayerId)
                    {
                        id = identity.IdentityId;
                    }
                }
            }

            if (MySession.Static.Players.TryGetPlayerId(id, out MyPlayer.PlayerId playerId))
            {
                if (MySession.Static.Players.TryGetPlayerById(playerId, out MyPlayer player))
                {
                    return player;
                }
            }

            return null;
        }
    }
}
