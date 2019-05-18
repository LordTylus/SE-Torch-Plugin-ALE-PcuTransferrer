using ALE_GridManager;
using NLog;
using Sandbox.Game.World;
using System.Collections.Generic;
using System.Reflection;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using static Sandbox.Game.World.MyBlockLimits;

namespace ALE_PcuTransferrer.Commands {
    public class PlayerCommands : CommandModule {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public GridManagerPlugin Plugin => (GridManagerPlugin)Context.Plugin;

        [Command("freebuild", "Increases block and PCU limits so you can paste big grids of multiple players.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void FreeBuild() {

            if (Context.Player == null) {
                Context.Respond("Not for Console");
                return;
            }

            MyPlayer player = Context.Player as MyPlayer;

            if (player == null) {
                Context.Respond("No player found!");
                return;
            }

            MyBlockLimits blockLimits = player.Identity.BlockLimits;

            Dictionary<string, short> limits = new Dictionary<string, short>(Context.Torch.CurrentSession.KeenSession.BlockTypeLimits);

            foreach (string pairName in limits.Keys) {

                MyTypeLimitData data = new MyTypeLimitData();
                data.BlockPairName = pairName;
                data.BlocksBuilt = -10000;

                blockLimits.SetTypeLimitsFromServer(data);
            }

            FieldInfo fieldInfo = blockLimits.GetType().GetField("m_PCU", BindingFlags.NonPublic | BindingFlags.Instance);

            if (fieldInfo == null) {
                Context.Respond("Unable to change PCUs try newer version. If you are the developer of the plugin. This is the moment you have to fix that!");
                return;
            }

            fieldInfo.SetValue(blockLimits, 1000000);

            fieldInfo = blockLimits.GetType().GetField("m_blocksBuilt", BindingFlags.NonPublic | BindingFlags.Instance);

            if (fieldInfo == null) {
                Context.Respond("Unable to change Blocks Build try newer version. If you are the developer of the plugin. This is the moment you have to fix that!");
                return;
            }

            fieldInfo.SetValue(blockLimits, -1000000);

            blockLimits.SetAllDirty();
        }
    }
}
