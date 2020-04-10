using ALE_Core.Utils;
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

namespace ALE_GridManager.Commands {

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

            if (!(Context.Player is MyPlayer player)) {
                Context.Respond("No player found!");
                return;
            }

            long playerId = player.Identity.IdentityId;

            var playersOnFreebuild = Plugin.PlayersOnFreebuild;

            int multiplier = 1;
            if (playersOnFreebuild.ContainsKey(playerId))
                multiplier = -1;

            MyBlockLimits blockLimits = player.Identity.BlockLimits;

            Dictionary<string, short> limits = new Dictionary<string, short>(Context.Torch.CurrentSession.KeenSession.BlockTypeLimits);

            foreach (string pairName in limits.Keys) {

                blockLimits.BlockTypeBuilt.TryGetValue(pairName, out MyTypeLimitData typeLimit);

                int blocksBuilt = 0;

                if (typeLimit != null)
                    blocksBuilt = typeLimit.BlocksBuilt;

                MyTypeLimitData data = new MyTypeLimitData {
                    BlockPairName = pairName,
                    BlocksBuilt = blocksBuilt - 10000 * multiplier
                };

                blockLimits.SetTypeLimitsFromServer(data);
            }

            int pcu = blockLimits.PCU;

            FieldInfo fieldInfo = blockLimits.GetType().GetField("m_PCU", BindingFlags.NonPublic | BindingFlags.Instance);

            if (fieldInfo == null) {
                Context.Respond("Unable to change PCUs try newer version. If you are the developer of the plugin. This is the moment you have to fix that!");
                return;
            }

            fieldInfo.SetValue(blockLimits, pcu + 1000000 * multiplier);

            int blocksBuild = blockLimits.BlocksBuilt;

            fieldInfo = blockLimits.GetType().GetField("m_blocksBuilt", BindingFlags.NonPublic | BindingFlags.Instance);

            if (fieldInfo == null) {
                Context.Respond("Unable to change Blocks Build try newer version. If you are the developer of the plugin. This is the moment you have to fix that!");
                return;
            }

            fieldInfo.SetValue(blockLimits, blocksBuild - 1000000 * multiplier);

            blockLimits.SetAllDirty();

            if (multiplier == 1) {

                playersOnFreebuild.TryAdd(playerId, playerId);

                Context.Respond("Freebuild enabled! (Run command again to disable)");

            } else {

                playersOnFreebuild.Remove(playerId);

                Context.Respond("Freebuild disabled!");
            }
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
            sb.AppendLine(blockLimits.BlocksBuilt.ToString("#,##0") + " / " + (blockLimits.BlockLimitModifier + blockLimits.MaxBlocks).ToString("#,##0") + " Blocks ("+ (blockLimits.BlockLimitModifier + blockLimits.MaxBlocks - blockLimits.BlocksBuilt).ToString("#,##0") + " Remaining)");
            sb.AppendLine(blockLimits.PCUBuilt.ToString("#,##0") + " / " + (blockLimits.PCU + blockLimits.PCUBuilt).ToString("#,##0") + " PCU (" + blockLimits.PCU.ToString("#,##0") + " Remaining)");
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
