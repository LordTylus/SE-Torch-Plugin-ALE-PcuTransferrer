using ALE_Core;
using ALE_Core.Cooldown;
using ALE_Core.Utils;
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

namespace ALE_GridManager.Commands {

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

        [Command("transfernobody", "Transfers PCU and Ownership of a ship over to nobody.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void TransferNobody() {
            TransferNobodyInternal(true, true);
        }

        [Command("transferpcunobody", "Transfers PCU of a ship over to nobody.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void TransferPcuNobody() {
            TransferNobodyInternal(true, false);
        }

        [Command("transferownernobody", "Transfers owner of a ship over to nobody.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void TransferOwnerNobody() {
            TransferNobodyInternal(false, true);
        }

        public void TransferInternal(bool pcu, bool ownership, bool force) {

            List<string> args = Context.Args;

            if (args.Count == 1) 
                TransferLookedAt(args[0], pcu, ownership, force);

            else if (args.Count == 2) 
                TransferGridName(args[1], args[0], pcu, ownership, force);

            else 
                Context.Respond("Correct Usage is !transfer <playerName> [gridName]");
        }

        public void TransferNobodyInternal(bool pcu, bool ownership) {

            List<string> args = Context.Args;

            if (args.Count == 0) 
                TransferNobodyLookedAt(pcu, ownership);

            else if (args.Count == 1)
                TransferNobodyGridName(args[0], pcu, ownership);

            else 
                Context.Respond("Correct Usage is !transfernobody [gridName]");
         }

        public void TransferGridName(string gridName, string playerName, bool pcu, bool ownership, bool force) {

            ulong steamId = PlayerUtils.GetSteamId(Context.Player);

            MyIdentity author = PlayerUtils.GetIdentityByName(playerName);

            if (!CheckConformation(steamId, author, gridName, null, pcu, force))
                return;

            try {

                Plugin.TransferModule.Transfer(gridName, author, Context, pcu, ownership, force);

            } catch (Exception e) {
                Log.Error(e, "Error on transferring ship");
            }
        }

        public void TransferLookedAt(string playerName, bool pcu, bool ownership, bool force) {

            ulong steamId = PlayerUtils.GetSteamId(Context.Player);

            if (Context.Player == null) {
                Context.Respond("Console has no Character so cannot use this command. Use !transfer <playerName> <gridname> instead!");
                return;
            }

            IMyCharacter character = Context.Player.Character;

            if (character == null) {
                Context.Respond("You have no Character currently. Make sure to spawn and be out of cockpit!");
                return;
            }

            MyIdentity author = PlayerUtils.GetIdentityByName(playerName);

            if (!CheckConformation(steamId, author, "nogrid_" + pcu + "_" + ownership, character, pcu, force))
                return;

            try {

                Plugin.TransferModule.Transfer(character, author, Context, pcu, ownership, force);

            } catch (Exception e) {
                Log.Error(e, "Error on transferring ship");
            }
        }

        public void TransferNobodyGridName(string gridName, bool pcu, bool ownership) {

            ulong steamId = PlayerUtils.GetSteamId(Context.Player);

            if (!CheckConformationNobody(steamId, gridName, null))
                return;

            try {

                Plugin.TransferModule.TransferNobody(gridName, Context, pcu, ownership);

            } catch (Exception e) {
                Log.Error(e, "Error on transferring ship");
            }
        }

        public void TransferNobodyLookedAt(bool pcu, bool ownership) {

            ulong steamId = PlayerUtils.GetSteamId(Context.Player);

            if (Context.Player == null) {
                Context.Respond("Console has no Character so cannot use this command. Use !transfernobody <gridname> instead!");
                return;
            }

            IMyCharacter character = Context.Player.Character;

            if (character == null) {
                Context.Respond("You have no Character currently. Make sure to spawn and be out of cockpit!");
                return;
            }

            if (!CheckConformationNobody(steamId, "nogrid_" + pcu + "_" + ownership, character))
                return;

            try {

                Plugin.TransferModule.TransferNobody(character, Context, pcu, ownership);

            } catch (Exception e) {
                Log.Error(e, "Error on transferring ship");
            }
        }

        private bool CheckConformation(ulong steamId, MyIdentity author, string gridName, IMyCharacter character, bool pcu, bool force) {

            if (author == null) {
                Context.Respond("Player not Found!");
                return false;
            }
            
            long authorId = author.IdentityId;

            var cooldownManager = Plugin.CooldownManager;
            var cooldownKey = new SteamIdCooldownKey(steamId);
            string command = gridName + "_" + authorId;

            if (!cooldownManager.CheckCooldown(cooldownKey, command, out _)) {
                cooldownManager.StopCooldown(cooldownKey);
                return true;
            }

            if (!CheckGridFound(author, gridName, character, pcu, force))
                return false;

            Context.Respond("Are you sure you want to continue? Enter the command again within 30 seconds to confirm.");
            cooldownManager.StartCooldown(cooldownKey, command, Plugin.CooldownConfirmation);
            
            return false;
        }

        private bool CheckConformationNobody(ulong steamId, string gridName, IMyCharacter character) {

            var cooldownManager = Plugin.CooldownManager;
            var cooldownKey = new SteamIdCooldownKey(steamId);
            string command = gridName + "_" + 0;

            if (!cooldownManager.CheckCooldown(cooldownKey, command, out _)) {
                cooldownManager.StopCooldown(cooldownKey);
                return true;
            }

            if (!CheckGridFoundNobody(gridName, character))
                return false;

            Context.Respond("Are you sure you want to continue? Enter the command again within 30 seconds to confirm.");
            cooldownManager.StartCooldown(cooldownKey, command, Plugin.CooldownConfirmation);

            return false;
        }

        private bool CheckGridFound(MyIdentity player, string gridName, IMyCharacter character, bool pcu, bool force) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups;

            if (character == null)
                groups = GridFinder.FindGridGroup(gridName);
            else
                groups = GridFinder.FindLookAtGridGroup(character);

            if (!Plugin.GroupCheckModule.CheckGroups(groups, out _, Context, player, pcu, force))
                return false;

            return true;
        }

        private bool CheckGridFoundNobody(string gridName, IMyCharacter character) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups;

            if (character == null)
                groups = GridFinder.FindGridGroup(gridName);
            else
                groups = GridFinder.FindLookAtGridGroup(character);

            if (!Plugin.GroupCheckModule.CheckGroupsNobody(groups, out _, Context))
                return false;

            return true;
        }
    }
}
