using ALE_Core.Utils;
using NLog;
using Sandbox.Game.World;
using System;
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

        [Command("checkusage player", "Lists how many Blocks and PCU each player has.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void CheckUsagePerPlayer() {

            List<string> args = Context.Args;

            string factionTag = null;
            string orderby = "blocks";
            bool showNPCs = false;
            bool online = false;
            long minPCU = 1;
            long minBlocks = 1;

            for (int i = 0; i < args.Count; i++) {

                if (args[i] == "-npc")
                    showNPCs = true;

                if (args[i] == "-online")
                    online = true;

                if (args[i].StartsWith("-faction="))
                    factionTag = args[i].Replace("-faction=", "");
                
                if (args[i].StartsWith("-orderby="))
                    orderby = args[i].Replace("-orderby=", "");

                if (args[i].StartsWith("-minpcu="))
                    long.TryParse(args[i].Replace("-minpcu=", ""), out minPCU);

                if (args[i].StartsWith("-minblocks="))
                    long.TryParse(args[i].Replace("-minblocks=", ""), out minBlocks);
            }

            if (orderby != "blocks" && orderby != "pcu" && orderby != "name") {
                Context.Respond("You can only order by 'pcu', 'name' or 'blocks'! Will use blocks as default.");
                orderby = "blocks";
            }

            List<KeyValuePair<string, BlocksAndPCU>> list = new List<KeyValuePair<string, BlocksAndPCU>>();

            foreach (MyIdentity identity in MySession.Static.Players.GetAllIdentities()) {

                if (!showNPCs && PlayerUtils.IsNpc(identity.IdentityId))
                    continue;

                if (online && !MySession.Static.Players.IsPlayerOnline(identity.IdentityId))
                    continue;

                if (factionTag != null && FactionUtils.GetPlayerFactionTag(identity.IdentityId) != factionTag)
                    continue;

                var blockLimits = identity.BlockLimits;
                var blocksAndPcu = new BlocksAndPCU(blockLimits.BlocksBuilt, blockLimits.PCUBuilt);

                list.Add(new KeyValuePair<string, BlocksAndPCU>(identity.DisplayName, blocksAndPcu));
            }

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Usage for Players");
            sb.AppendLine("---------------------------------------");

            OutputList(list, sb, orderby, minBlocks, minPCU, "players");
        }

        [Command("checkusage faction", "Lists how many Blocks and PCU each faction has.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void CheckUsagePerFaction() {

            List<string> args = Context.Args;

            string orderby = "blocks";
            bool showNPCs = false;
            long minPCU = 1;
            long minBlocks = 1;

            for (int i = 0; i < args.Count; i++) {

                if (args[i] == "-npc")
                    showNPCs = true;

                if (args[i].StartsWith("-orderby="))
                    orderby = args[i].Replace("-orderby=", "");

                if (args[i].StartsWith("-minpcu="))
                    long.TryParse(args[i].Replace("-minpcu=", ""), out minPCU);

                if (args[i].StartsWith("-minblocks="))
                    long.TryParse(args[i].Replace("-minblocks=", ""), out minBlocks);
            }

            if (orderby != "blocks" && orderby != "pcu" && orderby != "name") {
                Context.Respond("You can only order by 'pcu', 'name' or 'blocks'! Will use blocks as default.");
                orderby = "blocks";
            }

            List<KeyValuePair<string, BlocksAndPCU>> list = new List<KeyValuePair<string, BlocksAndPCU>>();

            foreach (IMyFaction faction in MySession.Static.Factions.Factions.Values) {

                if (!showNPCs && faction.IsEveryoneNpc())
                    continue;

                long blocksBuild = 0;
                long pcuBuild = 0;

                foreach(long identityId in faction.Members.Keys) {

                    MyIdentity identity = PlayerUtils.GetIdentityById(identityId);

                    var blockLimits = identity.BlockLimits;

                    blocksBuild += blockLimits.BlocksBuilt;
                    pcuBuild += blockLimits.PCUBuilt;
                }


                var blocksAndPcu = new BlocksAndPCU(blocksBuild, pcuBuild);
                var name = "[" + faction.Tag + "] " + faction.Name;

                list.Add(new KeyValuePair<string, BlocksAndPCU>(name, blocksAndPcu));
            }

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Usage for Factions");
            sb.AppendLine("---------------------------------------");

            OutputList(list, sb, orderby, minBlocks, minPCU, "factions");
        }

        private void OutputList(List<KeyValuePair<string, BlocksAndPCU>> list, StringBuilder sb, string orderby, long minBlocks, long minPCU, string forString) {

            list.Sort(delegate (KeyValuePair<string, BlocksAndPCU> pair1, KeyValuePair<string, BlocksAndPCU> pair2) {

                if (orderby == "name")
                    return pair1.Key.CompareTo(pair2.Key);

                if (orderby == "pcu")
                    return pair2.Value.PCU.CompareTo(pair1.Value.PCU);

                return pair2.Value.Blocks.CompareTo(pair1.Value.Blocks);
            });

            foreach(KeyValuePair<string, BlocksAndPCU> pair in list) {

                long blocks = pair.Value.Blocks;
                long pcu = pair.Value.PCU;

                if (pcu < minPCU || blocks < minBlocks)
                    continue;

                sb.AppendLine(pair.Key);
                sb.AppendLine("    Blocks: " + blocks);
                sb.AppendLine("    PCU: " + pcu);
            }

            if (Context.Player == null) {

                Context.Respond($"Usage for {forString}");
                Context.Respond(sb.ToString());

            } else {

                ModCommunication.SendMessageTo(new DialogMessage("Usages", $"Usages for {forString}", sb.ToString()), Context.Player.SteamUserId);
            }
        }

        private struct BlocksAndPCU {

            public long Blocks { get; }
            public long PCU { get; }

            public BlocksAndPCU(long blocksBuilt, long pcuBuilt) {
                Blocks = blocksBuilt;
                PCU = pcuBuilt;
            }
        }
    }
}
