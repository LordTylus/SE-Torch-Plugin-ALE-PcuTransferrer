using ALE_Core.Utils;
using Sandbox.Game.Entities;
using System.Collections.Concurrent;
using Torch.Commands;
using VRage.Game.ModAPI;
using VRage.Groups;

namespace ALE_GridManager.Modules {

    public class RechargeModule {

        private readonly GroupCheckModule groupCheckModule;

        public RechargeModule(GroupCheckModule groupCheckModule) {
            this.groupCheckModule = groupCheckModule;
        }

        public bool Recharge(IMyCharacter character, string blockpairName, float fillPercentage, CommandContext Context) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = GridFinder.FindLookAtGridGroup(character);

            return Recharge(groups, blockpairName, fillPercentage, Context);
        }

        public bool Recharge(string gridName, string blockpairName, float fillPercentage, CommandContext Context) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = GridFinder.FindGridGroup(gridName);

            return Recharge(groups, blockpairName, fillPercentage, Context);
        }

        private bool Recharge(ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups, string blockpairName, float fillPercentage, CommandContext Context) {

            if(blockpairName != "battery" 
                && blockpairName != "jumpdrive" 
                && blockpairName != "o2tank" 
                && blockpairName != "h2tank"
                && blockpairName != "tank") {

                Context.Respond("Only the following blocks are supported: battery, jumpdrive, o2tank, h2tank, tank");

                return false;
            }


            if (!groupCheckModule.CheckGroups(groups, out MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, Context, null, false, false))
                return false;

            return GridUtils.Recharge(group, blockpairName, fillPercentage, Context);
        }
    }
}
