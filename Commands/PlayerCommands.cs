using ALE_GridManager;
using ALE_PcuTransferrer.Utils;
using NLog;
using Sandbox.Game.World;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
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

        [Command("checklimits", "Lets you Peak into the Limits of the given Player.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void CheckLimits(string playerName) {

            MyIdentity identity = PlayerUtils.GetIdentityByName(playerName);

            if(identity == null) {
                Context.Respond("Player not found!");
                return;
            }

            MyBlockLimits blockLimits = identity.BlockLimits;

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Block Limits");
            sb.AppendLine("---------------------------------------");
            sb.AppendLine(blockLimits.BlocksBuilt.ToString("#,##0") + " / " + (blockLimits.BlockLimitModifier + blockLimits.MaxBlocks).ToString("#,##0") + " Blocks");
            sb.AppendLine(blockLimits.PCU.ToString("#,##0") + " / " + (blockLimits.PCU + blockLimits.PCUBuilt).ToString("#,##0") + " PCU");
            sb.AppendLine();
            sb.AppendLine("Block Type Limits");
            sb.AppendLine("---------------------------------------");

            Dictionary<string, short> globalLimits = Context.Torch.CurrentSession.KeenSession.BlockTypeLimits;

            foreach (string blockType in globalLimits.Keys) {

                MyTypeLimitData limit = blockLimits.BlockTypeBuilt[blockType];

                sb.AppendLine(blockType + " "+ limit.BlocksBuilt.ToString("#,##0") + " / " + globalLimits[blockType].ToString("#,##0") + " Blocks");
            }

            if (Context.Player == null) {

                Context.Respond($"Limits for {playerName}");
                Context.Respond(sb.ToString());

            } else {

                ModCommunication.SendMessageTo(new DialogMessage("Blocklimits", $"Limits for {playerName}", sb.ToString()), Context.Player.SteamUserId);
            }
        }
    }
}
