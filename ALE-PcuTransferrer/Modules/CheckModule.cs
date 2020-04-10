using ALE_Core.Utils;
using Sandbox.Game.Entities;
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

            if (!groupCheckModule.CheckGroups(groups, out MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, Context, null, false, false))
                return;

            GridUtils.CheckAuthor(group, Context);
        }
    }
}
