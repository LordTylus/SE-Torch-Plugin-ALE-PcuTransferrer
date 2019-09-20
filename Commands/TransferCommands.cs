using ALE_GridManager;
using ALE_PcuTransferrer.Utils;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using VRage.Groups;

namespace ALE_PcuTransferrer.Commands {
    public class TransferCommands : CommandModule {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public GridManagerPlugin Plugin => (GridManagerPlugin)Context.Plugin;

        [Command("transfer", "Transfers PCU and Ownership of a ship over to a specified player. It Respects any block and PCU limits.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void Transfer() {
            TransferInternal(true, true, false);
        }

        [Command("forcetransfer", "Transfers PCU and Ownership of a ship over to a specified player ignoring limits.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void ForceTransfer() {
            TransferInternal(true, true, true);
        }

        [Command("transferpcu", "Transfers PCU of a ship over to a specified player. It Respects any block and PCU limits.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void TransferPcu() {
            TransferInternal(true, false, false);
        }

        [Command("forcetransferpcu", "Transfers PCU of a ship over to a specified player ignoring limits.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void ForceTransferPcu() {
            TransferInternal(true, false, true);
        }

        [Command("transferowner", "Transfers Owner of a ship over to a specified player.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void TransferOwner() {
            TransferInternal(false, true, false);
        }

        public void TransferInternal(bool pcu, bool ownership, bool force) {

            List<String> args = Context.Args;

            if (args.Count == 1) {

                TransferLookedAt(args[0], pcu, ownership, force);

            } else if (args.Count == 2) {

                TransferGridName(args[1], args[0], pcu, ownership, force);

            } else {

                if (args.Count != 2)
                    Context.Respond("Correct Usage is !transfer <playerName> [gridName]");
            }
        }

        public void TransferGridName(string gridName, string playerName, bool pcu, bool ownership, bool force) {

            long playerId = 0L;

            if (Context.Player != null)
                playerId = Context.Player.IdentityId;

            MyIdentity author = PlayerUtils.GetIdentityByName(playerName);

            if (!CheckConformation(playerId, author, gridName, null, pcu, force))
                return;

            try {

                Plugin.Transfer(gridName, author, Context, pcu, ownership, force);

            } catch (Exception e) {
                Log.Error(e, "Error on transferring ship");
            }
        }

        public void TransferLookedAt(string playerName, bool pcu, bool ownership, bool force) {

            IMyPlayer player = Context.Player;

            long playerId;

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

            MyIdentity author = PlayerUtils.GetIdentityByName(playerName);

            if (!CheckConformation(playerId, author, "nogrid_" + pcu + "_" + ownership, character, pcu, force))
                return;

            try {

                Plugin.Transfer(character, author, Context, pcu, ownership, force);

            } catch (Exception e) {
                Log.Error(e, "Error on transferring ship");
            }
        }

        private bool CheckConformation(long executingPlayerId, MyIdentity author, string gridName, IMyCharacter character, bool pcu, bool force) {

            if (author == null && pcu) {
                Context.Respond("Player not Found!");
                return false;
            }

            long authorId = 0L;
            if (author != null)
                authorId = author.IdentityId;

            string command = gridName + "_" + authorId;

            var confirmationCooldownMap = Plugin.ConfirmationsMap;

            if (confirmationCooldownMap.TryGetValue(executingPlayerId, out CurrentCooldown confirmationCooldown)) {

                long remainingSeconds = confirmationCooldown.getRemainingSeconds(command);

                if (remainingSeconds == 0) {

                    if (!CheckGridFound(author, gridName, character, pcu, force))
                        return false;

                    Context.Respond("Are you sure you want to continue? Enter the command again within 30 seconds to confirm.");
                    confirmationCooldown.startCooldown(command);
                    return false;
                }

            } else {

                if (!CheckGridFound(author, gridName, character, pcu, force))
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

        private bool CheckGridFound(MyIdentity player, string gridName, IMyCharacter character, bool pcu, bool force) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups;

            if (character == null)
                groups = GridFinder.findGridGroup(gridName);
            else
                groups = GridFinder.findLookAtGridGroup(character);

            if (!GridManagerPlugin.CheckGroups(groups, out _, Context, player, pcu, force))
                return false;

            return true;
        }
    }
}
