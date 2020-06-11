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
using Sandbox.Game.Entities.Cube;
using ALE_Core.Cooldown;

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

        [Command("deleteblocks ownedby", "Deletes Blocks ownedby a certain player. (pass 'allgrids' to delete the blocks from all grids)")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void DeleteBlocksOwnedBy(string playerName, string gridname) {

            if (gridname == "allgrids")
                gridname = null;

            DeleteBlocks(playerName, true, gridname);
        }

        [Command("deleteblocks buildby", "Deletes Blocks builtby a certain player. (pass 'allgrids' to delete the blocks from all grids)")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void DeleteBlocksBuildBy(string playerName, string gridname) {

            if (gridname == "allgrids")
                gridname = null;

            DeleteBlocks(playerName, false, gridname);
        }

        [Command("deleteblocks noauthor", "Deletes Blocks which have no author. (pass 'allgrids' to delete the blocks from all grids)")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void DeleteBlocksNoAuthor(string gridname) {

            if (gridname == "allgrids")
                gridname = null;

            DeleteBlocksNoAuthorInternal(gridname);
        }

        private void DeleteBlocksNoAuthorInternal(string gridname) {

            ulong steamId = PlayerUtils.GetSteamId(Context.Player);

            if (!CheckConformation(steamId, 0L, false, gridname))
                return;

            List<MyCubeGrid> grids = new List<MyCubeGrid>(MyEntities.GetEntities().OfType<MyCubeGrid>().ToList());
            List<MySlimBlock> blocks = new List<MySlimBlock>();

            int deleteCount = 0;
            int deleteGridCount = 0;

            HashSet<long> identities = new HashSet<long>();

            foreach (var identity in MySession.Static.Players.GetAllIdentities())
                identities.Add(identity.IdentityId);

            foreach (MyCubeGrid grid in grids) {

                if (gridname != null && gridname != grid.DisplayName)
                    continue;

                blocks.Clear();
                blocks.AddRange(grid.GetBlocks());

                bool didDeleteSomethingFromGrid = false;

                foreach (var block in blocks) {

                    bool delete = false;

                    long buildBy = block.BuiltBy;

                    if (!identities.Contains(buildBy))
                        delete = true;

                    if (delete) {

                        grid.RazeBlock(block.Position);

                        deleteCount++;

                        didDeleteSomethingFromGrid = true;
                    }
                }

                if (didDeleteSomethingFromGrid)
                    deleteGridCount++;
            }

            Context.Respond($"Deleted {deleteCount} blocks from {deleteGridCount} grids.");
        }

        private void DeleteBlocks(string playerName, bool owned, string gridname) {

            ulong steamId = PlayerUtils.GetSteamId(Context.Player);

            MyIdentity player = PlayerUtils.GetIdentityByName(playerName);

            if (player == null) {
                Context.Respond($"Player {playerName} not found!");
                return;
            }

            long playerId = player.IdentityId;

            if (!CheckConformation(steamId, playerId, owned, gridname))
                return;

            List<MyCubeGrid> grids = new List<MyCubeGrid>(MyEntities.GetEntities().OfType<MyCubeGrid>().ToList());
            List<MySlimBlock> blocks = new List<MySlimBlock>();

            int deleteCount = 0;
            int deleteGridCount = 0;

            foreach (MyCubeGrid grid in grids) {

                if (gridname != null && gridname != grid.DisplayName)
                    continue;

                blocks.Clear();
                blocks.AddRange(grid.GetBlocks());

                bool didDeleteSomethingFromGrid = false;

                foreach(var block in blocks) {

                    bool delete = false;

                    if (owned) {

                        var fatBlock = block.FatBlock;
                        if (fatBlock != null && fatBlock.OwnerId == playerId)
                            delete = true;

                    } else if (block.BuiltBy == playerId) {
                        delete = true;
                    }

                    if (delete) {

                        grid.RazeBlock(block.Position);
                        
                        deleteCount++;
                        
                        didDeleteSomethingFromGrid = true;
                    }
                }

                if (didDeleteSomethingFromGrid)
                    deleteGridCount++;
            }

            Context.Respond($"Deleted {deleteCount} blocks from {deleteGridCount} grids.");
        }

        private bool CheckConformation(ulong steamId, long playerId, bool owned, string gridname) {

            string command = playerId + "_" + owned + "_" + gridname;

            var cooldownManager = Plugin.CooldownManager;
            var cooldownKey = new SteamIdCooldownKey(steamId);

            if (!cooldownManager.CheckCooldown(cooldownKey, command, out _)) {
                cooldownManager.StopCooldown(cooldownKey);
                return true;
            }

            Context.Respond("Are you sure you want to continue? Enter the command again within 30 seconds to confirm.");
            cooldownManager.StartCooldown(cooldownKey, command, Plugin.CooldownConfirmation);

            return false;
        }
    }
}
