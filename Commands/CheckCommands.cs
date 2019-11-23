using NLog;
using System;
using System.Collections.Generic;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace ALE_GridManager.Commands {
    public class CheckCommands : CommandModule {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public GridManagerPlugin Plugin => (GridManagerPlugin)Context.Plugin;

        [Command("checkowner", "Checks the owner of the grid.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void CheckOwner() {

            List<string> args = Context.Args;

            if (args.Count == 0) {

                CheckOwnerLookedAt();

            } else if (args.Count == 1) {

                CheckOwnerGridName(args[0]);

            } else {

                if (args.Count != 2)
                    Context.Respond("Correct Usage is !checkowner [gridName]");
            }
        }

        public void CheckOwnerGridName(string gridName) {

            try {

                Plugin.CheckOwner(gridName, Context);

            } catch (Exception e) {
                Log.Error(e, "Error on transferring ship");
            }
        }

        public void CheckOwnerLookedAt() {

            IMyPlayer player = Context.Player;

            if (player == null) {

                Context.Respond("Console has no Character so cannot use this command. Use !checkowner <gridname> instead!");
                return;
            }

            IMyCharacter character = player.Character;

            if (character == null) {
                Context.Respond("You have no Character currently. Make sure to spawn and be out of cockpit!");
                return;
            }

            try {

                Plugin.CheckOwner(character, Context);

            } catch (Exception e) {
                Log.Error(e, "Error on transferring ship");
            }
        }

        [Command("checkauthor", "Checks the author (PCU) of the grid.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void CheckAuthor() {

            List<string> args = Context.Args;

            if (args.Count == 0) {

                CheckAuthorLookedAt();

            } else if (args.Count == 1) {

                CheckAuthorGridName(args[0]);

            } else {

                if (args.Count != 2)
                    Context.Respond("Correct Usage is !checkauthor [gridName]");
            }
        }

        public void CheckAuthorGridName(string gridName) {

            try {

                Plugin.CheckAuthor(gridName, Context);

            } catch (Exception e) {
                Log.Error(e, "Error on transferring ship");
            }
        }

        public void CheckAuthorLookedAt() {

            IMyPlayer player = Context.Player;

            if (player == null) {

                Context.Respond("Console has no Character so cannot use this command. Use !checkauthor <gridname> instead!");
                return;
            }

            IMyCharacter character = player.Character;

            if (character == null) {
                Context.Respond("You have no Character currently. Make sure to spawn and be out of cockpit!");
                return;
            }

            try {

                Plugin.CheckAuthor(character, Context);

            } catch (Exception e) {
                Log.Error(e, "Error on transferring ship");
            }
        }
    }
}
