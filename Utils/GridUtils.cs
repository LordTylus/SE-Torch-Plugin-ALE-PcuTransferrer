using NLog;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using Torch.Commands;
using VRage.Game;
using VRage.Groups;
using VRage.Network;
using VRage.ObjectBuilders;

namespace ALE_PcuTransferrer.Utils {

    public class GridUtils {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static bool Repair(MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, CommandContext Context) {

            foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in group.Nodes) {

                MyCubeGrid grid = groupNodes.NodeData;

                var gridOwnerList = grid.BigOwners;
                var ownerCnt = gridOwnerList.Count;
                var gridOwner = 0L;

                if (ownerCnt > 0 && gridOwnerList[0] != 0)
                    gridOwner = gridOwnerList[0];
                else if (ownerCnt > 1)
                    gridOwner = gridOwnerList[1];

                HashSet<MySlimBlock> blocks = grid.GetBlocks();
                foreach (MySlimBlock block in blocks) {

                    long owner = block.OwnerId;
                    if (owner == 0)
                        owner = gridOwner;

                    if (block.CurrentDamage > 0 || block.HasDeformation) {

                        block.ClearConstructionStockpile(null);
                        block.IncreaseMountLevel(block.MaxIntegrity, owner, null, 10000, true);

                        MyCubeBlock cubeBlock = block.FatBlock;

                        if (cubeBlock != null) {

                            grid.ChangeOwnerRequest(grid, cubeBlock, 0, MyOwnershipShareModeEnum.Faction);
                            if (owner != 0)
                                grid.ChangeOwnerRequest(grid, cubeBlock, owner, MyOwnershipShareModeEnum.Faction);
                        }
                    }
                }
            }

            Context.Respond("Grid was repaired!");
            return true;
        }

        public static bool Transfer(MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, CommandContext Context, MyIdentity newAuthor, bool pcu, bool ownership) {

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

                                if(!knownIdentities.Contains(oldAuthor)) {

                                    var identity = PlayerUtils.GetIdentityById(oldAuthor);

                                    if(identity == null) {

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

        public static bool TransferNobody(MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, CommandContext Context, bool pcu, bool ownership) {

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

        public static void CheckOwner(MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, CommandContext Context) {

            foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in group.Nodes) {

                MyCubeGrid grid = groupNodes.NodeData;

                if (grid.Physics == null)
                    continue;

                Dictionary<long, int> blocksPerAuthorMap = new Dictionary<long, int>();

                HashSet<MySlimBlock> blocks = new HashSet<MySlimBlock>(grid.GetBlocks());
                foreach (MySlimBlock block in blocks) {

                    if (block == null || block.CubeGrid == null || block.IsDestroyed)
                        continue;

                    if (block.FatBlock != null) {

                        long ownerId = block.FatBlock.OwnerId;

                        if (blocksPerAuthorMap.ContainsKey(ownerId))
                            blocksPerAuthorMap[ownerId] += 1;
                        else
                            blocksPerAuthorMap.Add(ownerId, 1);
                    }
                }

                Context.Respond("Owners at grid: " + grid.DisplayName);

                List<KeyValuePair<long, int>> myList = blocksPerAuthorMap.ToList();

                myList.Sort(delegate (KeyValuePair<long, int> pair1, KeyValuePair<long, int> pair2) {
                    return pair2.Value.CompareTo(pair1.Value);
                });

                foreach (KeyValuePair<long, int> pair in myList)
                    Context.Respond("   " + PlayerUtils.GetPlayerNameById(pair.Key) + " = " + pair.Value + " blocks");
            }
        }

        public static void CheckAuthor(MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, CommandContext Context) {

            foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in group.Nodes) {

                MyCubeGrid grid = groupNodes.NodeData;

                if (grid.Physics == null)
                    continue;

                Dictionary<long, int> blocksPerAuthorMap = new Dictionary<long, int>();

                HashSet<MySlimBlock> blocks = new HashSet<MySlimBlock>(grid.GetBlocks());
                foreach (MySlimBlock block in blocks) {

                    if (block == null || block.CubeGrid == null || block.IsDestroyed)
                        continue;

                    int pcu = BlockUtils.GetPcu(block);
                    long ownerId = block.BuiltBy;

                    if (blocksPerAuthorMap.ContainsKey(ownerId))
                        blocksPerAuthorMap[ownerId] += pcu;
                    else
                        blocksPerAuthorMap.Add(ownerId, pcu);
                }

                Context.Respond("Authors at grid: " + grid.DisplayName);

                List<KeyValuePair<long, int>> myList = blocksPerAuthorMap.ToList();

                myList.Sort(delegate (KeyValuePair<long, int> pair1, KeyValuePair<long, int> pair2) {
                    return pair2.Value.CompareTo(pair1.Value);
                });

                foreach (KeyValuePair<long, int> pair in myList) 
                    Context.Respond("   " + PlayerUtils.GetPlayerNameById(pair.Key) + " = " + pair.Value + " PCU");
            }
        }
    }
}
