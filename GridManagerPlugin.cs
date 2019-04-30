using NLog;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Concurrent;
using VRage.Groups;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using Torch;
using Torch.API;
using VRage.Game.ModAPI;
using System.Collections.Generic;
using Torch.Commands;
using System.Linq;
using Sandbox.Game.Entities.Cube;
using VRage.Game;
using Sandbox.Game.World;
using static Sandbox.Game.World.MyBlockLimits;
using Sandbox.Engine.Multiplayer;
using VRage.Network;
using System;
using Task = System.Threading.Tasks.Task;
using Parallel = ParallelTasks.Parallel;

namespace ALE_GridManager {

    public class GridManagerPlugin : TorchPluginBase {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private Dictionary<long, CurrentCooldown> _confirmations = new Dictionary<long, CurrentCooldown>();

        public Dictionary<long, CurrentCooldown> ConfirmationsMap { get { return _confirmations; } }

        public long CooldownConfirmation { get { return 30 * 1000; } }

        /// <inheritdoc />
        public override void Init(ITorchBase torch) {
            base.Init(torch);
        }

        public bool repair(IMyCharacter character, CommandContext Context) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = FindLookAtGridGroup(character);

            return repair(groups, Context);
        }

        public bool repair(string gridName, CommandContext Context) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = findGridGroups(gridName);

            return repair(groups, Context);
        }

        public bool transfer(IMyCharacter character, MyPlayer newAuthor, CommandContext Context, bool pcu, bool ownership) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = FindLookAtGridGroup(character);

            return transfer(groups, Context, newAuthor, pcu, ownership);
        }

        public bool transfer(string gridName, MyPlayer newAuthor, CommandContext Context, bool pcu, bool ownership) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = findGridGroups(gridName);

            return transfer(groups, Context, newAuthor, pcu, ownership);
        }

        public static bool checkGroups(ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups, 
            out MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, CommandContext Context, 
            MyPlayer newAuthor, bool pcu) {

            /* No group or too many groups found */
            if (groups.Count < 1) {

                Context.Respond("Could not find the Grid.");
                group = null;

                return false;
            }

            /* too many groups found */
            if (groups.Count > 1) {

                Context.Respond("Found multiple Grids with same Name. Make sure the name is unique.");
                group = null;

                return false;
            }

            if (!groups.TryPeek(out group)) {
                Context.Respond("Could not work with found grid for unknown reason.");
                return false;
            }

            if (pcu) {

                var blockLimits = newAuthor.Identity.BlockLimits;

                if (!checkLimits(group, blockLimits, Context, newAuthor))
                    return false;
            }

            return true;
        }

        private bool repair(ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups, CommandContext Context) {

            MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group = null;

            if (!checkGroups(groups, out group, Context, null, false))
                return false;

            return repair(group, Context);
        }

        private bool repair(MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, CommandContext Context) {

            List<MyObjectBuilder_EntityBase> objectBuilderList = new List<MyObjectBuilder_EntityBase>();

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

        private bool transfer(ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups,
            CommandContext Context, MyPlayer newAuthor, bool pcu, bool ownership) {

            MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group = null;

            if (!checkGroups(groups, out group, Context, newAuthor, pcu))
                return false;

            return transfer(group, Context, newAuthor, pcu, ownership);
        }

        private bool transfer(MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, CommandContext Context, MyPlayer newAuthor, bool pcu, bool ownership) {

            if (!pcu && !ownership) { 
                Context.Respond("The plugindev did an oopsie and nothing was changed!");
                return false;
            }

            long newAuthorId = newAuthor.Identity.IdentityId;

            List<MyCubeGrid> grids = new List<MyCubeGrid>();

            foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in group.Nodes) 
                grids.Add(groupNodes.NodeData);

            try {

                if (pcu && ownership)
                    Context.Respond("Start transferring PCU and Ownership to "+newAuthor.DisplayName+"!");
                else if (pcu)
                    Context.Respond("Start transferring PCU to " + newAuthor.DisplayName + "!");
                else if (ownership)
                    Context.Respond("Start transferring Ownership to " + newAuthor.DisplayName + "!");

                foreach (MyCubeGrid grid in grids) {

                    HashSet<long> authors = new HashSet<long>();

                    HashSet<MySlimBlock> blocks = new HashSet<MySlimBlock>(grid.GetBlocks());
                    foreach (MySlimBlock block in blocks) {

                        if (block == null || block.CubeGrid == null || block.IsDestroyed )
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

                            if (block.BuiltBy == 0) {

                                /* 
                                * Hack: TransferBlocksBuiltByID only transfers authorship if it has an author. 
                                * Transfer Authorship Client just sets the author so we need to take care of limits ourselves. 
                                */
                                block.TransferAuthorshipClient(newAuthorId);
                                block.AddAuthorship();
                            }

                            authors.Add(block.BuiltBy);
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
                Log.Error("Error on transferring ship", e);
            }

            return true;
        }

        public static ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> FindLookAtGridGroup(IMyCharacter controlledEntity) {

            const float range = 5000;
            Matrix worldMatrix;
            Vector3D startPosition;
            Vector3D endPosition;

            worldMatrix = controlledEntity.GetHeadMatrix(true, true, false); // dead center of player cross hairs, or the direction the player is looking with ALT.
            startPosition = worldMatrix.Translation + worldMatrix.Forward * 0.5f;
            endPosition = worldMatrix.Translation + worldMatrix.Forward * (range + 0.5f);

            var entites = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entites, e => e != null);

            var list = new Dictionary<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group, double>();
            var ray = new RayD(startPosition, worldMatrix.Forward);

            foreach(var group in MyCubeGridGroups.Static.Physical.Groups) {

                foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in group.Nodes) {

                    IMyCubeGrid cubeGrid = groupNodes.NodeData;

                    if (cubeGrid != null) {

                        if (cubeGrid.Physics == null)
                            continue;

                        // check if the ray comes anywhere near the Grid before continuing.    
                        if (ray.Intersects(cubeGrid.WorldAABB).HasValue) {

                            Vector3I? hit = cubeGrid.RayCastBlocks(startPosition, endPosition);

                            if (hit.HasValue) {

                                double distance = (startPosition - cubeGrid.GridIntegerToWorld(hit.Value)).Length();

                                double oldDistance;

                                if (list.TryGetValue(group, out oldDistance)) {

                                    if (distance < oldDistance) {
                                        list.Remove(group);
                                        list.Add(group, distance);
                                    }

                                } else {

                                    list.Add(group, distance);
                                }
                            }
                        }
                    }
                }
            }

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> bag = new ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group>();

            if (list.Count == 0) 
                return bag;

            // find the closest Entity.
            var item = list.OrderBy(f => f.Value).First();
            bag.Add(item.Key);

            return bag;
        }

        public static ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> findGridGroups(string gridName) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = new ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group>();
            Parallel.ForEach(MyCubeGridGroups.Static.Physical.Groups, group => {

                foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in group.Nodes) {

                    IMyCubeGrid grid = groupNodes.NodeData;

                    if (grid.Physics == null)
                        continue;

                    /* Gridname is wrong ignore */
                            if (!grid.CustomName.Equals(gridName))
                        continue;

                    groups.Add(group);
                }
            });

            return groups;
        }

        private static bool checkLimits(MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, 
            MyBlockLimits blockLimits, CommandContext Context, MyPlayer newAuthor) {

            Dictionary<string, short> limits = new Dictionary<string, short>(Context.Torch.CurrentSession.KeenSession.BlockTypeLimits);

            foreach (string blockType in blockLimits.BlockTypeBuilt.Keys) {

                MyTypeLimitData limit = blockLimits.BlockTypeBuilt[blockType];

                if (!limits.ContainsKey(blockType))
                    continue;

                short remainingBlocks = (short) (limits[blockType] - limit.BlocksBuilt);

                limits.Remove(blockType);
                limits.Add(blockType, remainingBlocks);
            }

            long authorId = newAuthor.Identity.IdentityId;
            long pcusOfGroup = 0L;
            long blockCountOfGroup = 0L;

            foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in group.Nodes) {

                MyCubeGrid grid = groupNodes.NodeData;

                HashSet<MySlimBlock> blocks = grid.GetBlocks();
                foreach (MySlimBlock block in blocks) {

                    if (block.BuiltBy != authorId) {

                        int pcu = 1;
                        if (block.ComponentStack.IsFunctional)
                            pcu = block.BlockDefinition.PCU;

                        pcusOfGroup += pcu;
                        blockCountOfGroup++;

                        string blockType = block.BlockDefinition.BlockPairName;

                        if (!limits.ContainsKey(blockType))
                            continue;

                        short remainingBlocks = (short)(limits[blockType] - 1);

                        if(remainingBlocks < 0) {
                            Log.Info("Player '" + newAuthor.DisplayName + "' does not have high enough Limit for Block Type " + blockType + "!");
                            Context.Respond("Player does not have high enough Limit for Block Type "+ blockType + "!");
                            return false;
                        }

                        limits.Remove(blockType);
                        limits.Add(blockType, remainingBlocks);
                    }
                }
            }

            if (blockLimits.MaxBlocks < blockLimits.BlocksBuilt + blockCountOfGroup) {

                Log.Info("Player '" + newAuthor.DisplayName + "' does not have a high enough Blocklimit! " +
                    "(Max: " + blockLimits.MaxBlocks + ", " +
                    "Built: " + blockLimits.BlocksBuilt + ", " +
                    "Grids: "+ blockCountOfGroup + ")");

                Context.Respond("Player does not have a high enough Blocklimit!");
                return false;
            }

            if (blockLimits.PCU < pcusOfGroup) {

                Log.Info("Player '" + newAuthor.DisplayName + "' does not have a high enough PCU limit! " +
                    "(Remaining: " + blockLimits.PCU + ", " +
                    "Built: " + blockLimits.PCUBuilt + ", " +
                    "Grids: " + pcusOfGroup + ")");

                Context.Respond("Player does not have a high enough PCU limit!");
                return false;
            }

            return true;
        }
    }
}