using NLog;
using System;
using System.Collections.Generic;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace ALE_GridManager.Commands {

    public class RepairCommands : CommandModule {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public GridManagerPlugin Plugin => (GridManagerPlugin)Context.Plugin;

        [Command("repair", "Repairs grid to full integrity.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void Repair() {

            List<string> args = Context.Args;

            if (args.Count == 0) {

                RepairLookedAt();

            } else if (args.Count == 1) {

                RepairGridName(args[0]);

            } else {

                if (args.Count != 2)
                    Context.Respond("Correct Usage is !repair [gridName]");
            }
        }

        public void RepairGridName(string gridName) {

            try {

                Plugin.RepairModule.Repair(gridName, Context);

                Context.Respond("Grid was repaired!");

            } catch (Exception e) {
                Log.Error(e, "Error on transferring ship");
            }
        }

        public void RepairLookedAt() {

            IMyPlayer player = Context.Player;

            if (player == null) {

                Context.Respond("Console has no Character so cannot use this command. Use !repair <gridname> instead!");
                return;
            } 

            IMyCharacter character = player.Character;

            if (character == null) {
                Context.Respond("You have no Character currently. Make sure to spawn and be out of cockpit!");
                return;
            }

            try {

                Plugin.RepairModule.Repair(character, Context);

                Context.Respond("Grid was repaired!");

            } catch (Exception e) {
                Log.Error(e, "Error on transferring ship");
            }
        }
    }
}
