using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using VRage.Groups;

namespace ALE_GridManager {

    public class Commands : CommandModule {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public GridManagerPlugin Plugin => (GridManagerPlugin) Context.Plugin;


        [Command("transfer", "Transfers PCU and Ownership of a ship over to a specified player.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void Transfer() {
            TransferInternal(true, true);
        }

        [Command("transferpcu", "Transfers PCU of a ship over to a specified player.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void TransferPcu() {
            TransferInternal(true, false);
        }

        [Command("transferowner", "Transfers Owner of a ship over to a specified player.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void TransferOwner() {
            TransferInternal(false, true);
        }

        public void TransferInternal(bool pcu, bool ownership) {

            List<String> args = Context.Args;

            if (args.Count == 1) {

                TransferLookedAt(args[0], pcu, ownership);

            } else if(args.Count == 2) { 

                TransferGridName(args[0], args[1], pcu, ownership);

            } else {

                if (args.Count != 2)
                    Context.Respond("Correct Usage is !transfer <playerName> [gridName]");
            }
        }

        public void TransferGridName(string gridName, string playerName, bool pcu, bool ownership) {

            long playerId = 0L;

            if (Context.Player != null)
                playerId = Context.Player.IdentityId;

            MyPlayer author = GetPlayerByNameOrId(playerName);

            if (!checkConformation(playerId, author, gridName, null, pcu))
                return;

            try {

                Plugin.transfer(gridName, author, Context, pcu, ownership);

            } catch (Exception e) {
                Log.Error("Error on transferring ship", e);
            }
        }

        public void TransferLookedAt(string playerName, bool pcu, bool ownership) {

            IMyPlayer player = Context.Player;

            long playerId = 0L;

            if (player == null) {

                Context.Respond("Console has no Character so cannot use this command. Use !transfer <playerName> <gridname> instead!");
                return;

            } else {
                playerId = player.IdentityId;
            }

            IMyCharacter character = player.Character;

            if (character == null) {
                Context.Respond("You have no Character currently. Make sure to spawn and be out of cockpit!");
                return;
            }

            MyPlayer author = GetPlayerByNameOrId(playerName);

            if (!checkConformation(playerId, author, "nogrid_" + pcu + "_" + ownership, character, pcu))
                return;

            try {

                Plugin.transfer(character, author, Context, pcu, ownership);

            } catch (Exception e) {
                Log.Error("Error on transferring ship", e);
            }
        }

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

            if (!checkConformation(playerId, null, gridName, null, false))
                return;

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

                Context.Respond("Console has no Character so cannot use this command. Use !transfer <playerName> <gridname> instead!");
                return;

            } else {
                playerId = player.IdentityId;
            }

            IMyCharacter character = player.Character;

            if (character == null) {
                Context.Respond("You have no Character currently. Make sure to spawn and be out of cockpit!");
                return;
            }

            if (!checkConformation(playerId, null, "nogridrepair", character, false))
                return;

            try {

                Plugin.repair(character, Context);

            } catch (Exception e) {
                Log.Error("Error on transferring ship", e);
            }
        }


        private bool checkConformation(long executingPlayerId, MyPlayer author, string gridName, IMyCharacter character, bool pcu) {

            if (author == null && pcu) {
                Context.Respond("Player not Found!.");
                return false;
            }

            long authorId = 0L;
            if (author != null)
                authorId = author.Identity.IdentityId;

            string command = gridName + "_" + authorId;

            var confirmationCooldownMap = Plugin.ConfirmationsMap;

            CurrentCooldown confirmationCooldown = null;

            if (confirmationCooldownMap.TryGetValue(executingPlayerId, out confirmationCooldown)) {

                long remainingSeconds = confirmationCooldown.getRemainingSeconds(command);

                if (remainingSeconds == 0) {

                    if (!checkGridFound(author, gridName, character, pcu))
                        return false;

                    Context.Respond("Are you sure you want to continue? Enter the command again within 30 seconds to confirm.");
                    confirmationCooldown.startCooldown(command);
                    return false;
                }

            } else {

                if (!checkGridFound(author, gridName, character, pcu))
                    return false;

                confirmationCooldown = new CurrentCooldown(Plugin.CooldownConfirmation);
                confirmationCooldownMap.Add(executingPlayerId, confirmationCooldown);

                Context.Respond("Are you sure you want to continue? Enter the command again within 30 seconds to confirm.");

                confirmationCooldown.startCooldown(command);
                return false;
            }

            confirmationCooldownMap.Remove(executingPlayerId);
            return true;
        }

        private bool checkGridFound(MyPlayer player, string gridName, IMyCharacter character, bool pcu) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups;

            if (character == null)
                groups = GridManagerPlugin.findGridGroups(gridName);
            else
                groups = GridManagerPlugin.FindLookAtGridGroup(character);

            MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group = null;

            if (!GridManagerPlugin.checkGroups(groups, out group, Context, player, pcu))
                return false;

            return true;
        }

        public static MyPlayer GetPlayerByNameOrId(string nameOrPlayerId) {

            if (!long.TryParse(nameOrPlayerId, out long id)) {
                foreach (var identity in MySession.Static.Players.GetAllIdentities()) {
                    if (identity.DisplayName == nameOrPlayerId) {
                        id = identity.IdentityId;
                    }
                }
            }

            if (MySession.Static.Players.TryGetPlayerId(id, out MyPlayer.PlayerId playerId)) {
                if (MySession.Static.Players.TryGetPlayerById(playerId, out MyPlayer player)) {
                    return player;
                }
            }

            return null;
        }
    }
}
