using ALE_GridManager;
using ALE_PcuTransferrer.Utils;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace ALE_PcuTransferrer.Commands {
    public class StatisticCommands : CommandModule {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public GridManagerPlugin Plugin => (GridManagerPlugin)Context.Plugin;

        [Command("listblocks", "Lists how Many Blocks of Type/Subtype are present in your World.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void ListBlocks() {

            List<string> args = Context.Args;

            if (args.Count >= 1) {

                string type = args[0];

                bool countPcu = false;
                string factionTag = null;
                string playerName = null;

                for (int i = 1; i < args.Count; i++) {

                    if (args[i] == "-pcu")
                        countPcu = true;

                    if (args[i].StartsWith("-faction="))
                        factionTag = args[i].Replace("-faction=", "");

                    if (args[i].StartsWith("-player="))
                        playerName = args[i].Replace("-player=", "");
                }

                if (type == "all") 
                    ListBlocks(false, countPcu, factionTag, playerName);
                else if (type == "limited") 
                    ListBlocks(true, countPcu, factionTag, playerName);
                else 
                    Context.Respond("Known type only 'all' and 'limited' is supported!");

            } else {

                Context.Respond("Correct Usage is !listblocks <all|limited> [-pcu] [-player=<playerName>] [-faction=<factionTag>]");
            }
        }

        private void ListBlocks(bool limitedOnly, bool countPCU, string factionTag, string playerName) {

            Dictionary<string, short> globalLimits = Context.Torch.CurrentSession.KeenSession.BlockTypeLimits;
            Dictionary<string, long> blockCounts = new Dictionary<string, long>();
            Dictionary<string, long> pcuCounts = new Dictionary<string, long>();

            int gridCount = 0;

            HashSet<long> identities = null;

            string title = "Blocks in World";

            if (playerName != null) {

                MyIdentity player = PlayerUtils.GetIdentityByName(playerName);
                if (player == null) {

                    Context.Respond("Player not found!");
                    return;
                }

                title = "Block of Player " + playerName;

                identities = new HashSet<long>();
                identities.Add(player.IdentityId);

            } else if (factionTag != null) {

                IMyFaction faction = FactionUtils.GetIdentityByTag(factionTag);

                if (faction == null) {

                    Context.Respond("Faction not found!");
                    return;
                }

                title = "Block of Faction " + factionTag;

                identities = new HashSet<long>();
                foreach (long identityId in faction.Members.Keys)
                    identities.Add(identityId);
            }

            foreach (MyEntity entity in MyEntities.GetEntities()) {

                MyCubeGrid grid = entity as MyCubeGrid;

                if (grid == null)
                    continue;

                if (grid.Physics == null)
                    continue;

                HashSet<MySlimBlock> blocks = new HashSet<MySlimBlock>(grid.GetBlocks());

                bool countGrid = false;

                foreach (MySlimBlock block in blocks) {

                    if (block == null || block.CubeGrid == null || block.IsDestroyed)
                        continue;

                    if (identities != null && !identities.Contains(block.BuiltBy))
                        continue;

                    countGrid = true;

                    string pairName = block.BlockDefinition.BlockPairName;

                    if (!limitedOnly || globalLimits.ContainsKey(pairName)) {

                        if (!blockCounts.ContainsKey(pairName))
                            blockCounts.Add(pairName, 0);

                        if (!pcuCounts.ContainsKey(pairName))
                            pcuCounts.Add(pairName, 0);

                        blockCounts[pairName]++;
                        pcuCounts[pairName] += BlockUtils.getPcu(block);
                    }
                }

                if(countGrid)
                    gridCount++;
            }

            List<KeyValuePair<string, long>> myList = blockCounts.ToList();

            myList.Sort(delegate (KeyValuePair<string, long> pair1, KeyValuePair<string, long> pair2) {
                return pair2.Value.CompareTo(pair1.Value);
            });

            StringBuilder sb = new StringBuilder();

            long totalPCU = 0;
            long totalValue = 0;

            foreach (KeyValuePair<string, long> keyValuePair in myList) {

                string pcuString = "";

                if (countPCU) {

                    pcuString = " " + pcuCounts[keyValuePair.Key].ToString("#,##0") + " PCU";

                    totalPCU += pcuCounts[keyValuePair.Key];
                }

                if (limitedOnly)
                    sb.AppendLine(keyValuePair.Value.ToString("#,##0") + " / (" + globalLimits[keyValuePair.Key] + ")   " + keyValuePair.Key + pcuString);
                else
                    sb.AppendLine(keyValuePair.Value.ToString("#,##0") + "   " + keyValuePair.Key + pcuString);

                totalValue += keyValuePair.Value;
            }

            sb.AppendLine();
            sb.AppendLine("Total: " + totalValue.ToString("#,##0") + " Blocks");

            if (countPCU)
                sb.AppendLine("Total: " + totalPCU.ToString("#,##0") + " PCU");

            sb.AppendLine();
            sb.AppendLine(gridCount.ToString("#,##0") + " Grids checked");


            if (Context.Player == null) {

                Context.Respond(title);
                Context.Respond(sb.ToString());

            } else {

                String subtitle = "All Blocks";
                if (limitedOnly)
                    subtitle = "Only Limited Blocks";

                ModCommunication.SendMessageTo(new DialogMessage(title, subtitle, sb.ToString()), Context.Player.SteamUserId);
            }
        }
    }
}
