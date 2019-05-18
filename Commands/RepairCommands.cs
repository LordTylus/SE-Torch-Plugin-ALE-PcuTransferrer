﻿using NLog;
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

            List<String> args = Context.Args;

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

            long playerId = 0L;

            if (Context.Player != null)
                playerId = Context.Player.IdentityId;

            try {

                Plugin.repair(gridName, Context);

            } catch (Exception e) {
                Log.Error("Error on transferring ship", e);
            }
        }

        public void RepairLookedAt() {

            IMyPlayer player = Context.Player;

            long playerId = 0L;

            if (player == null) {

                Context.Respond("Console has no Character so cannot use this command. Use !repair <gridname> instead!");
                return;

            } else {
                playerId = player.IdentityId;
            }

            IMyCharacter character = player.Character;

            if (character == null) {
                Context.Respond("You have no Character currently. Make sure to spawn and be out of cockpit!");
                return;
            }

            try {

                Plugin.repair(character, Context);

            } catch (Exception e) {
                Log.Error("Error on transferring ship", e);
            }
        }
    }
}