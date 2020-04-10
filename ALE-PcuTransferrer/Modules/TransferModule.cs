using ALE_Core.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using VRage.Game.ModAPI;
using VRage.Groups;

namespace ALE_GridManager.Modules {

    public class TransferModule {

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

            if (!groupCheckModule.CheckGroupsNobody(groups, out MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, Context))
                return false;

            return GridUtils.TransferNobody(group, Context, pcu, ownership);
        }
    }
}
