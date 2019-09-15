using ALE_GridManager;
using ALE_PcuTransferrer.Utils;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;
using VRage.Groups;
using static Sandbox.Game.World.MyBlockLimits;

namespace ALE_PcuTransferrer.Commands {
    public class ProtectCommands : CommandModule {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public GridManagerPlugin Plugin => (GridManagerPlugin)Context.Plugin;

        [Command("protect", "Makes Grid indistructable.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void Protect() {

            List<string> args = Context.Args;

            string gridname = null;
            bool allowEdit = false;
            bool allowDamage = false;

            for (int i = 0; i < args.Count; i++) {

                if (args[i] == "-allowDamage") {
                    allowDamage = true;
                    continue;
                }

                if (args[i] == "-allowEdit") {
                    allowEdit = true;
                    continue;
                }

                gridname = args[i];
            }

            if (gridname == null) 
                ProtectLookedAt(allowEdit, allowDamage);
            else
                ProtectGridName(args[0], allowEdit, allowDamage);
        }

        public void ProtectGridName(string gridName, bool allowEdit, bool allowDamage) {

            try {

                Protect(gridName, Context, allowEdit, allowDamage);

            } catch (Exception e) {
                Log.Error(e, "Error on Protecting grid");
            }
        }

        public void ProtectLookedAt(bool allowEdit, bool allowDamage) {

            IMyPlayer player = Context.Player;

            if (player == null) {

                Context.Respond("Console has no Character so cannot use this command. Use !protect <gridname> instead!");
                return;
            }

            IMyCharacter character = player.Character;

            if (character == null) {
                Context.Respond("You have no Character currently. Make sure to spawn and be out of cockpit!");
                return;
            }

            try {

                Protect(character, Context, allowEdit, allowDamage);

            } catch (Exception e) {
                Log.Error(e, "Error on Protecting grid");
            }
        }

        [Command("unprotect", "Makes Grid indistructable.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void Unprotect() {

            List<string> args = Context.Args;

            if (args.Count == 0) {

                UnprotectLookedAt();

            } else if (args.Count == 1) {

                UnprotectGridName(args[0]);

            } else {

                if (args.Count != 2)
                    Context.Respond("!protect [gridname]");
            }
        }

        public void UnprotectGridName(string gridName) {

            try {

                Unprotect(gridName, Context);

            } catch (Exception e) {
                Log.Error(e, "Error on Unprotecting grid");
            }
        }

        public void UnprotectLookedAt() {

            IMyPlayer player = Context.Player;

            if (player == null) {

                Context.Respond("Console has no Character so cannot use this command. Use !unprotect <gridname> instead!");
                return;
            }

            IMyCharacter character = player.Character;

            if (character == null) {
                Context.Respond("You have no Character currently. Make sure to spawn and be out of cockpit!");
                return;
            }

            try {

                Unprotect(character, Context);

            } catch (Exception e) {
                Log.Error(e, "Error on Unprotecting grid");
            }
        }

        internal void Protect(string gridName, CommandContext context, bool allowEdit, bool allowDamage) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group> groups = GridFinder.findGridGroupMechanical(gridName);

            Protect(groups, context, allowEdit, allowDamage);
        }

        internal void Protect(IMyCharacter character, CommandContext context, bool allowEdit, bool allowDamage) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group> groups = GridFinder.findLookAtGridGroupMechanical(character);

            Protect(groups, context, allowEdit, allowDamage);
        }

        private void Protect(ConcurrentBag<MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group> groups, CommandContext Context, bool allowEdit, bool allowDamage) {

            MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group group;

            if (!CheckGroups(groups, out group, Context))
                return;

            foreach (MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Node groupNodes in group.Nodes) {

                MyCubeGrid grid = groupNodes.NodeData;

                if(!allowDamage)
                    grid.DestructibleBlocks = false;

                if (!allowEdit)
                    grid.Editable = false;
            }

            Context.Respond("Grid is now protected!");
        }

        internal void Unprotect(string gridName, CommandContext context) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group> groups = GridFinder.findGridGroupMechanical(gridName);

            Unprotect(groups, context);
        }

        internal void Unprotect(IMyCharacter character, CommandContext context) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group> groups = GridFinder.findLookAtGridGroupMechanical(character);

            Unprotect(groups, context);
        }

        private void Unprotect(ConcurrentBag<MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group> groups, CommandContext Context) {

            MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group group;

            if (!CheckGroups(groups, out group, Context))
                return;

            foreach (MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Node groupNodes in group.Nodes) {

                MyCubeGrid grid = groupNodes.NodeData;

                grid.DestructibleBlocks = true;
                grid.Editable = true;
            }

            Context.Respond("Grid is no longer protected!");
        }

        public static bool CheckGroups(ConcurrentBag<MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group> groups,
                out MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group group, CommandContext Context) {

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
    }
}
