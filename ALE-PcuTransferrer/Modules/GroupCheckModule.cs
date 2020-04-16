using ALE_GridManager.Modules.Limits;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Torch.Commands;
using VRage.Groups;

namespace ALE_GridManager.Modules {

    public class GroupCheckModule {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public List<ILimitChecker> limitCheckers = new List<ILimitChecker>();

        public void AddLimitChecker(ILimitChecker limitChecker) {
            this.limitCheckers.Add(limitChecker);
        }

        public bool CheckGroups(ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups,
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

                if (!CheckLimits(group, Context, newAuthor))
                    return false;
            }

            return true;
        }

        public bool CheckGroupsNobody(ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups,
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

        private bool CheckLimits(MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group,
            CommandContext Context, MyIdentity newAuthor) {
            
            List<MySlimBlock> allBlocks = GetAllBlocksForGroup(group, newAuthor);

            foreach (ILimitChecker limitChecker in limitCheckers) {

                var response = limitChecker.CheckLimits(allBlocks, newAuthor);

                if(!response.BlockLimitFine) {

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Player '" + newAuthor.DisplayName + "' does not have a high enough Blocklimit!");
                    sb.AppendLine("Max: " + response.BlockLimit);
                    sb.AppendLine("Built: " + response.CurrentBlocks);
                    sb.AppendLine("After Transfer: " + response.BlockLimitAfterTransfer);
                    sb.AppendLine("Vertified with: " + limitChecker.GetName());

                    string logMessage = sb.ToString();

                    Log.Info(logMessage);
                    Context.Respond(logMessage);

                    return false;
                }

                if(!response.PcuFine) {

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Player '" + newAuthor.DisplayName + "' does not have a high enough PCU limit!");
                    sb.AppendLine("Max: " + response.PcuLimit);
                    sb.AppendLine("Built: " + response.CurrentPcu);
                    sb.AppendLine("After Transfer: " + response.PcuAfterTransfer);
                    sb.AppendLine("Vertified with: " + limitChecker.GetName());

                    string logMessage = sb.ToString();

                    Log.Info(logMessage);
                    Context.Respond(logMessage);

                    return false;
                }

                if (!response.TypeLimitsFine) {

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Player '" + newAuthor.DisplayName + "' does not have high enough Limit for following Block Types:");
                    
                    foreach(var overLimitBlock in response.OverLimitBlocks)
                        sb.AppendLine(overLimitBlock.Key.BlockPairName +": "+ overLimitBlock.Value + " too many!");

                    sb.AppendLine("Vertified with: " + limitChecker.GetName());

                    string logMessage = sb.ToString();

                    Log.Info(logMessage);
                    Context.Respond(logMessage);

                    return false;
                }
            }

            return true;
        }

        private static List<MySlimBlock> GetAllBlocksForGroup(MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, MyIdentity newAuthor) {
            
            long authorId = newAuthor.IdentityId;

            List<MySlimBlock> allBlocks = new List<MySlimBlock>();

            foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in group.Nodes) {

                MyCubeGrid grid = groupNodes.NodeData;

                foreach (MySlimBlock block in grid.GetBlocks())
                    if (block.BuiltBy != authorId)
                        allBlocks.Add(block);
            }

            return allBlocks;
        }
    }
}
