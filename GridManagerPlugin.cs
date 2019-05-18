using NLog;
using Sandbox.Game.Entities;
using System.Collections.Concurrent;
using VRage.Groups;
using Torch;
using Torch.API;
using VRage.Game.ModAPI;
using System.Collections.Generic;
using Torch.Commands;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using static Sandbox.Game.World.MyBlockLimits;
using ALE_PcuTransferrer.Utils;
using ALE_PcuTransferrer;

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

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = GridFinder.findLookAtGridGroup(character);

            return repair(groups, Context);
        }

        public bool repair(string gridName, CommandContext Context) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = GridFinder.findGridGroup(gridName);

            return repair(groups, Context);
        }

        private bool repair(ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups, CommandContext Context) {

            MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group = null;

            if (!checkGroups(groups, out group, Context, null, false, false))
                return false;

            return GridUtils.repair(group, Context);
        }

        public bool transfer(IMyCharacter character, MyPlayer newAuthor, CommandContext Context, bool pcu, bool ownership, bool force) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = GridFinder.findLookAtGridGroup(character);

            return transfer(groups, Context, newAuthor, pcu, ownership, force);
        }

        public bool transfer(string gridName, MyPlayer newAuthor, CommandContext Context, bool pcu, bool ownership, bool force) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = GridFinder.findGridGroup(gridName);

            return transfer(groups, Context, newAuthor, pcu, ownership, force);
        }

        private bool transfer(ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups,
                CommandContext Context, MyPlayer newAuthor, bool pcu, bool ownership, bool force) {

            MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group = null;

            if (!checkGroups(groups, out group, Context, newAuthor, pcu, force))
                return false;

            return GridUtils.transfer(group, Context, newAuthor, pcu, ownership);
        }

        public void checkOwner(IMyCharacter character, CommandContext Context) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = GridFinder.findLookAtGridGroup(character);

            checkOwner(groups, Context);
        }

        public void checkOwner(string gridName, CommandContext Context) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = GridFinder.findGridGroup(gridName);

            checkOwner(groups, Context);
        }

        private void checkOwner(ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups, CommandContext Context) {

            MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group = null;

            if (!checkGroups(groups, out group, Context, null, false, false))
                return;

            GridUtils.checkOwner(group, Context);
        }

        public void checkAuthor(IMyCharacter character, CommandContext Context) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = GridFinder.findLookAtGridGroup(character);

            checkAuthor(groups, Context);
        }

        public void checkAuthor(string gridName, CommandContext Context) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = GridFinder.findGridGroup(gridName);

            checkAuthor(groups, Context);
        }

        private void checkAuthor(ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups, CommandContext Context) {

            MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group = null;

            if (!checkGroups(groups, out group, Context, null, false, false))
                return;

            GridUtils.checkAuthor(group, Context);
        }

        public static bool checkGroups(ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups, 
            out MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, CommandContext Context, 
            MyPlayer newAuthor, bool pcu, bool force) {

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

            if (pcu && !force) {

                var blockLimits = newAuthor.Identity.BlockLimits;

                if (!checkLimits(group, blockLimits, Context, newAuthor))
                    return false;
            }

            return true;
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