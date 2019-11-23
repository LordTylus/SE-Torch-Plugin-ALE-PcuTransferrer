using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using ALE_Core.Utils;
using Sandbox.Game;

namespace ALE_GridManager.Commands {
    public class GridCleanupCommands : CommandModule {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public GridManagerPlugin Plugin => (GridManagerPlugin)Context.Plugin;

        [Command("gridcleanup", "Cleans grids where owner is inactive for longer than x days.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void GridCleanup(int days) {

            DateTime timeDelta = DateTime.Now - TimeSpan.FromDays(days);

            int deletedGrids = 0;

            List<MyCubeGrid> grids = MyEntities.GetEntities().OfType<MyCubeGrid>().ToList();
            List<MyIdentity> identities = MySession.Static.Players.GetAllIdentities().ToList();

            HashSet<long> removedPlayers = new HashSet<long>();

            foreach (MyIdentity identity in identities) {

                long identityId = identity.IdentityId;

                /* Ignore NPCs */
                if (PlayerUtils.IsNpc(identityId))
                    continue;

                /* Use latest date. If player is not online logout time is used otherwise login time */
                DateTime referenceTime = identity.LastLoginTime;
                if (identity.LastLogoutTime > referenceTime)
                    referenceTime = identity.LastLogoutTime;

                /* Still active so ignore */
                if (referenceTime >= timeDelta)
                    continue;

                /* If player had a faction, kick him */
                MyVisualScriptLogicProvider.KickPlayerFromFaction(identityId);

                foreach (var grid in grids) {

                    if (grid.BigOwners.Contains(identityId)) {
                        grid.Close();
                        deletedGrids++;
                    }
                }

                removedPlayers.Add(identityId);
            }

            GridUtils.TransferBlocksToBigOwner(removedPlayers);

            Context.Respond($"Found {removedPlayers.Count} inactive players and deleted {deletedGrids} grids.");
        }
    }
}
