using NLog;
using System;
using System.Collections.Generic;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace ALE_GridManager.Commands {

    public class RechargeCommands : CommandModule {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public GridManagerPlugin Plugin => (GridManagerPlugin)Context.Plugin;

        [Command("recharge", "Recharges given block to given percentage on looked at grid.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void Recharge(string blockpairName, float fillPercentage = 100) {

            IMyPlayer player = Context.Player;

            if (player == null) {

                Context.Respond("Console has no Character so cannot use this command. Use !recharge <blockpair> <percentage> <gridname> instead!");
                return;
            }

            IMyCharacter character = player.Character;

            if (character == null) {
                Context.Respond("You have no Character currently. Make sure to spawn and be out of cockpit!");
                return;
            }

            try {

                fillPercentage /= 100;

                Plugin.RechargeModule.Recharge(character, blockpairName, fillPercentage, Context);

            } catch (Exception e) {
                Log.Error(e, "Error on recharging grid");
            }
        }

        [Command("recharge grid", "Recharges given block to given percentage on given grid.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void Recharge(string gridName, string blockpairName, float fillPercentage = 100) {

            try {

                fillPercentage /= 100;

                Plugin.RechargeModule.Recharge(gridName, blockpairName, fillPercentage, Context);

            } catch (Exception e) {
                Log.Error(e, "Error on recharging grid");
            }
        }
    }
}
