using ALE_Core.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Torch.Commands;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;
using VRage.Groups;

namespace ALE_GridManager.Modules {

    public class CheckModule {

        private readonly GroupCheckModule groupCheckModule;

        public CheckModule(GroupCheckModule groupCheckModule) {
            this.groupCheckModule = groupCheckModule;
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

            if (!groupCheckModule.CheckGroups(groups, out MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, Context, null, false, false))
                return;

            StringBuilder sb = new StringBuilder();

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

                sb.AppendLine("Owners at grid: " + grid.DisplayName);

                List<KeyValuePair<long, int>> myList = blocksPerAuthorMap.ToList();

                myList.Sort(delegate (KeyValuePair<long, int> pair1, KeyValuePair<long, int> pair2) {
                    return pair2.Value.CompareTo(pair1.Value);
                });

                foreach (KeyValuePair<long, int> pair in myList)
                    sb.AppendLine("   " + PlayerUtils.GetPlayerNameById(pair.Key) + " = " + pair.Value + " blocks");
            }

            if (Context.Player == null) {

                Context.Respond($"Owners of Grids");
                Context.Respond(sb.ToString());

            } else {

                ModCommunication.SendMessageTo(new DialogMessage("Owners of Grids", "", sb.ToString()), Context.Player.SteamUserId);
            }
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

            if (!groupCheckModule.CheckGroups(groups, out MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, Context, null, false, false))
                return;

            StringBuilder sb = new StringBuilder();

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

                sb.AppendLine("Authors at grid: " + grid.DisplayName);

                List<KeyValuePair<long, int>> myList = blocksPerAuthorMap.ToList();

                myList.Sort(delegate (KeyValuePair<long, int> pair1, KeyValuePair<long, int> pair2) {
                    return pair2.Value.CompareTo(pair1.Value);
                });

                foreach (KeyValuePair<long, int> pair in myList)
                    sb.AppendLine("   " + PlayerUtils.GetPlayerNameById(pair.Key) + " = " + pair.Value + " PCU");
            }

            if (Context.Player == null) {

                Context.Respond($"Authors of Grids");
                Context.Respond(sb.ToString());

            } else {

                ModCommunication.SendMessageTo(new DialogMessage("Authors of Grids", "", sb.ToString()), Context.Player.SteamUserId);
            }
        }
    }
}
