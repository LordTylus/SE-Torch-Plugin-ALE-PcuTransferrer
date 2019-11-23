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
using System.Windows.Controls;
using Torch.API.Plugins;
using ALE_GridManager.UI;
using ALE_Core.Utils;

namespace ALE_GridManager {

    public class GridManagerPlugin : TorchPluginBase, IWpfPlugin {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private UserControl _control;
        public UserControl GetControl() => _control ?? (_control = new CommandsUi());

        public Dictionary<long, CurrentCooldown> ConfirmationsMap { get; } = new Dictionary<long, CurrentCooldown>();
        public ConcurrentDictionary<long, long> PlayersOnFreebuild { get; } = new ConcurrentDictionary<long, long>();

        public long CooldownConfirmation { get { return 30 * 1000; } }

        /// <inheritdoc />
        public override void Init(ITorchBase torch) {
            base.Init(torch);
        }

        public bool Repair(IMyCharacter character, CommandContext Context) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = GridFinder.FindLookAtGridGroup(character);

            return Repair(groups, Context);
        }

        public bool Repair(string gridName, CommandContext Context) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = GridFinder.FindGridGroup(gridName);

            return Repair(groups, Context);
        }

        private bool Repair(ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups, CommandContext Context) {

            if (!CheckGroups(groups, out MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, Context, null, false, false))
                return false;

            return GridUtils.Repair(group, Context);
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


            if (!CheckGroups(groups, out MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, Context, newAuthor, pcu, force))
                return false;

            return GridUtils.Transfer(group, Context, newAuthor, pcu, ownership);
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


            if (!CheckGroupsNobody(groups, out MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, Context))
                return false;

            return GridUtils.TransferNobody(group, Context, pcu, ownership);
        }

        public void CheckOwner(IMyCharacter character, CommandContext Context) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = GridFinder.FindLookAtGridGroup(character);

            CheckOwner(groups, Context);
        }

        public void CheckOwner(string gridName, CommandContext Context) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = GridFinder.FindGridGroup(gridName);

            CheckOwner(groups, Context);
        }

        private void CheckOwner(ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups, CommandContext Context) {

            if (!CheckGroups(groups, out MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, Context, null, false, false))
                return;

            GridUtils.CheckOwner(group, Context);
        }

        public void CheckAuthor(IMyCharacter character, CommandContext Context) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = GridFinder.FindLookAtGridGroup(character);

            CheckAuthor(groups, Context);
        }

        public void CheckAuthor(string gridName, CommandContext Context) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = GridFinder.FindGridGroup(gridName);

            CheckAuthor(groups, Context);
        }

        private void CheckAuthor(ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups, CommandContext Context) {

            if (!CheckGroups(groups, out MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, Context, null, false, false))
                return;

            GridUtils.CheckAuthor(group, Context);
        }

        public static bool CheckGroups(ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups, 
            out MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, CommandContext Context,
            MyIdentity newAuthor, bool pcu, bool force) {

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

                var blockLimits = newAuthor.BlockLimits;

                if (!CheckLimits(group, blockLimits, Context, newAuthor))
                    return false;
            }

            return true;
        }


        public static bool CheckGroupsNobody(ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups,
            out MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, CommandContext Context) {

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

            return true;
        }

        private static bool CheckLimits(MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, 
            MyBlockLimits blockLimits, CommandContext Context, MyIdentity newAuthor) {

            Dictionary<string, short> limits = new Dictionary<string, short>(Context.Torch.CurrentSession.KeenSession.BlockTypeLimits);

            foreach (string blockType in blockLimits.BlockTypeBuilt.Keys) {

                MyTypeLimitData limit = blockLimits.BlockTypeBuilt[blockType];

                if (!limits.ContainsKey(blockType))
                    continue;

                short remainingBlocks = (short) (limits[blockType] - limit.BlocksBuilt);

                limits.Remove(blockType);
                limits.Add(blockType, remainingBlocks);
            }

            long authorId = newAuthor.IdentityId;
            long pcusOfGroup = 0L;
            long blockCountOfGroup = 0L;

            foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in group.Nodes) {

                MyCubeGrid grid = groupNodes.NodeData;

                HashSet<MySlimBlock> blocks = grid.GetBlocks();
                foreach (MySlimBlock block in blocks) {

                    if (block.BuiltBy != authorId) {

                        pcusOfGroup += BlockUtils.GetPcu(block);
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

            if (blockLimits.MaxBlocks > 0 && blockLimits.MaxBlocks < blockLimits.BlocksBuilt + blockCountOfGroup) {

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