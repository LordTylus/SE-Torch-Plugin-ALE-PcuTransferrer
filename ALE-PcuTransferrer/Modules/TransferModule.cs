using ALE_Core.Utils;
using NLog;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Torch.Commands;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Groups;
using VRage.Network;

namespace ALE_GridManager.Modules {

    public class TransferModule {

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly GroupCheckModule groupCheckModule;

        public TransferModule(GroupCheckModule groupCheckModule) {
            this.groupCheckModule = groupCheckModule;
        }

        public bool Transfer(IMyCharacter character, MyIdentity newAuthor, CommandContext Context, bool pcu, bool ownership, bool force) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = GridFinder.FindLookAtGridGroup(character);

            return Transfer(groups, Context, newAuthor, pcu, ownership, force);
        }

        public bool Transfer(string gridName, MyIdentity newAuthor, CommandContext Context, bool pcu, bool ownership, bool force) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = GridFinder.FindGridGroup(gridName);

            return Transfer(groups, Context, newAuthor, pcu, ownership, force);
        }

        private bool Transfer(ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups,
                CommandContext Context, MyIdentity newAuthor, bool pcu, bool ownership, bool force) {

            if (!groupCheckModule.CheckGroups(groups, out MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, Context, newAuthor, pcu, force))
                return false;

            if (!pcu && !ownership) {
                Context.Respond("The plugindev did an oopsie and nothing was changed!");
                return false;
            }

            HashSet<long> knownIdentities = new HashSet<long>();
            HashSet<long> unknownIdentities = new HashSet<long>();

            long newAuthorId = newAuthor.IdentityId;

            List<MyCubeGrid> grids = new List<MyCubeGrid>();

            foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in group.Nodes)
                grids.Add(groupNodes.NodeData);

            try {

                if (pcu && ownership)
                    Context.Respond("Start transferring PCU and Ownership to " + newAuthor.DisplayName + "!");
                else if (pcu)
                    Context.Respond("Start transferring PCU to " + newAuthor.DisplayName + "!");
                else if (ownership)
                    Context.Respond("Start transferring Ownership to " + newAuthor.DisplayName + "!");

                foreach (MyCubeGrid grid in grids) {

                    HashSet<long> authors = new HashSet<long>();

                    HashSet<MySlimBlock> blocks = new HashSet<MySlimBlock>(grid.GetBlocks());
                    foreach (MySlimBlock block in blocks) {

                        if (block == null || block.CubeGrid == null || block.IsDestroyed)
                            continue;

                        if (ownership) {

                            MyCubeBlock cubeBlock = block.FatBlock;
                            if (cubeBlock != null && cubeBlock.OwnerId != newAuthorId) {

                                grid.ChangeOwnerRequest(grid, cubeBlock, 0, MyOwnershipShareModeEnum.Faction);
                                if (newAuthorId != 0)
                                    grid.ChangeOwnerRequest(grid, cubeBlock, newAuthorId, MyOwnershipShareModeEnum.Faction);
                            }
                        }

                        if (pcu) {

                            long oldAuthor = block.BuiltBy;

                            bool forceTransfer = oldAuthor == 0 || unknownIdentities.Contains(oldAuthor);

                            if (!forceTransfer && oldAuthor != newAuthorId) {

                                if (!knownIdentities.Contains(oldAuthor)) {

                                    var identity = PlayerUtils.GetIdentityById(oldAuthor);

                                    if (identity == null) {

                                        unknownIdentities.Add(oldAuthor);
                                        forceTransfer = true;

                                    } else {

                                        knownIdentities.Add(oldAuthor);
                                    }
                                }
                            }

                            if (forceTransfer) {

                                /* 
                                * Hack: TransferBlocksBuiltByID only transfers authorship if it has an author. 
                                * Transfer Authorship Client just sets the author so we need to take care of limits ourselves. 
                                */
                                block.TransferAuthorshipClient(newAuthorId);
                                block.AddAuthorship();
                            }

                            authors.Add(oldAuthor);
                        }
                    }

                    foreach (long author in authors)
                        MyMultiplayer.RaiseEvent(grid, x => new Action<long, long>(x.TransferBlocksBuiltByID), author, newAuthorId, new EndpointId());
                }

                if (pcu && ownership)
                    Context.Respond("PCU and Ownership was transferred!");
                else if (pcu)
                    Context.Respond("PCU was transferred!");
                else if (ownership)
                    Context.Respond("Ownership was transferred!");

            } catch (Exception e) {
                Context.Respond("Error Transferring Ship!");
                Log.Error(e, "Error on transferring ship");
            }

            return true;
        }

        public bool TransferNobody(IMyCharacter character, CommandContext Context, bool pcu, bool ownership) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = GridFinder.FindLookAtGridGroup(character);

            return TransferNobody(groups, Context, pcu, ownership);
        }

        public bool TransferNobody(string gridName, CommandContext Context, bool pcu, bool ownership) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = GridFinder.FindGridGroup(gridName);

            return TransferNobody(groups, Context, pcu, ownership);
        }

        private bool TransferNobody(ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups,
                CommandContext Context, bool pcu, bool ownership) {

            if (!groupCheckModule.CheckGroupsNobody(groups, out MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, Context))
                return false;


            if (!pcu && !ownership) {
                Context.Respond("The plugindev did an oopsie and nothing was changed!");
                return false;
            }

            List<MyCubeGrid> grids = new List<MyCubeGrid>();

            foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in group.Nodes)
                grids.Add(groupNodes.NodeData);

            try {

                if (pcu && ownership)
                    Context.Respond("Start transferring PCU and Ownership to nobody!");
                else if (pcu)
                    Context.Respond("Start transferring PCU to nobody!");
                else if (ownership)
                    Context.Respond("Start transferring Ownership to nobody!");

                foreach (MyCubeGrid grid in grids) {

                    HashSet<long> authors = new HashSet<long>();

                    HashSet<MySlimBlock> blocks = new HashSet<MySlimBlock>(grid.GetBlocks());
                    foreach (MySlimBlock block in blocks) {

                        if (block == null || block.CubeGrid == null || block.IsDestroyed)
                            continue;

                        if (ownership) {

                            MyCubeBlock cubeBlock = block.FatBlock;
                            if (cubeBlock != null && cubeBlock.OwnerId != 0)
                                grid.ChangeOwnerRequest(grid, cubeBlock, 0, MyOwnershipShareModeEnum.Faction);
                        }

                        if (pcu) {

                            long oldAuthor = block.BuiltBy;

                            if (oldAuthor != 0) {

                                block.RemoveAuthorship();
                                block.TransferAuthorshipClient(0L);

                                authors.Add(oldAuthor);
                            }
                        }
                    }

                    foreach (long author in authors) {

                        MyIdentity identity = PlayerUtils.GetIdentityById(author);

                        if (identity == null)
                            continue;

                        identity.BlockLimits.SetAllDirty();
                        identity.BlockLimits.CallLimitsChanged();
                    }
                }

                if (pcu && ownership)
                    Context.Respond("PCU and Ownership was removed!");
                else if (pcu)
                    Context.Respond("PCU was removed!");
                else if (ownership)
                    Context.Respond("Ownership was removed!");

            } catch (Exception e) {
                Context.Respond("Error Transferring Ship!");
                Log.Error(e, "Error on transferring ship");
            }

            return true;
        }
    }
}
