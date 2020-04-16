using ALE_Core.Utils;
using Sandbox.Game.Entities;
using System.Collections.Concurrent;
using Torch.Commands;
using VRage.Game.ModAPI;
using VRage.Groups;

namespace ALE_GridManager.Modules {

    public class RepairModule {

        private readonly GroupCheckModule groupCheckModule;

        public RepairModule(GroupCheckModule groupCheckModule) {
            this.groupCheckModule = groupCheckModule;
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

            if (!groupCheckModule.CheckGroups(groups, out MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, Context, null, false, false))
                return false;

            return GridUtils.Repair(group, Context);
        }
    }
}
