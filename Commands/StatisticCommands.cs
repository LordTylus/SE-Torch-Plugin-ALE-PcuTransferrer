using ALE_Core.Utils;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using System.Collections.Generic;
using System.Text;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace ALE_GridManager.Commands {

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
                string gridName = null;
                string orderby = "blocks";

                for (int i = 1; i < args.Count; i++) {

                    if (args[i] == "-pcu")
                        countPcu = true;

                    if (args[i].StartsWith("-faction="))
                        factionTag = args[i].Replace("-faction=", "");

                    if (args[i].StartsWith("-player="))
                        playerName = args[i].Replace("-player=", "");

                    if (args[i].StartsWith("-grid="))
                        gridName = args[i].Replace("-grid=", "");

                    if (args[i].StartsWith("-orderby="))
                        orderby = args[i].Replace("-orderby=", "");
                }

                if (orderby != "blocks" && orderby != "pcu" && orderby != "name") {
                    Context.Respond("You can only order by 'pcu', 'name' or 'blocks'! Will use blocks as default.");
                    orderby = "blocks";
                }

                if (type == "all") 
                    ListBlocks(false, countPcu, factionTag, playerName, gridName, orderby);
                else if (type == "limited") 
                    ListBlocks(true, countPcu, factionTag, playerName, gridName, orderby);
                else 
                    Context.Respond("Known type only 'all' and 'limited' is supported!");

            } else {

                Context.Respond("Correct Usage is !listblocks <all|limited> [-pcu] [-player=<playerName>] [-faction=<factionTag>] [-orderby=<pcu|name|blocks>]");
            }
        }

        private void ListBlocks(bool limitedOnly, bool countPCU, string factionTag, string playerName, string gridName, string orderby) {

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

                identities = new HashSet<long> {
                    player.IdentityId
                };

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

                if (!(entity is MyCubeGrid grid))
                    continue;

                if (grid.Physics == null)
                    continue;

                if (gridName != null && grid.DisplayName != gridName)
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
                        pcuCounts[pairName] += BlockUtils.GetPcu(block);
                    }
                }

                if(countGrid)
                    gridCount++;
            }

            List<string> myList = new List<string>(blockCounts.Keys);

            myList.Sort(delegate (string pair1, string pair2) {

                if(orderby == "name") 
                    return pair1.CompareTo(pair2);

                if (orderby == "pcu")
                    return pcuCounts[pair2].CompareTo(pcuCounts[pair1]);

                return blockCounts[pair2].CompareTo(blockCounts[pair1]);
            });

            StringBuilder sb = new StringBuilder();

            long totalPCU = 0;
            long totalValue = 0;

            foreach (string pair in myList) {

                string pcuString = "";

                if (countPCU) {

                    long pcu = pcuCounts[pair];

                    pcuString = " " + pcu.ToString("#,##0") + " PCU";

                    totalPCU += pcu;
                }

                long blocks = blockCounts[pair];

                if (limitedOnly)
                    sb.AppendLine(blocks.ToString("#,##0").PadRight(7) + " / (" + globalLimits[pair] + ")   " + pair + pcuString);
                else
                    sb.AppendLine(blocks.ToString("#,##0").PadRight(7) + "   " + pair + pcuString);

                totalValue += blocks;
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

                string subtitle = "All Blocks";
                if (limitedOnly)
                    subtitle = "Only Limited Blocks";

                ModCommunication.SendMessageTo(new DialogMessage(title, subtitle, sb.ToString()), Context.Player.SteamUserId);
            }
        }

        [Command("findblock", "Lists which Faction/Player has a defined block in the world. ")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void FindBlocks() {

            List<string> args = Context.Args;

            if (args.Count >= 1) {

                string type = args[0];

                string factionTag = null;
                string playerName = null;
                string groupby = "player";

                for (int i = 1; i < args.Count; i++) {

                    if (args[i].StartsWith("-faction="))
                        factionTag = args[i].Replace("-faction=", "");

                    if (args[i].StartsWith("-player="))
                        playerName = args[i].Replace("-player=", "");

                    if (args[i].StartsWith("-groupby="))
                        groupby = args[i].Replace("-groupby=", "");
                }

                if (groupby != "player" && groupby != "faction") {
                    Context.Respond("You can only group by 'player', 'faction'! Will use player as default.");
                    groupby = "player";
                }

                FindBlocks(type, factionTag, playerName, groupby);

            } else {

                Context.Respond("Correct Usage is !findblock <blockpairname> [-player=<playerName>] [-faction=<factionTag>] [-groupby=<player|faction>]");
            }
        }

        private void FindBlocks(string type, string factionTag, string playerName, string groupby) {

            Dictionary<string, long> blockCounts = new Dictionary<string, long>();

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

                identities = new HashSet<long> {
                    player.IdentityId
                };

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
                
                if (!(entity is MyCubeGrid grid))
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

                    string pairName = block.BlockDefinition.BlockPairName;

                    if (pairName == null || pairName.ToLower() != type.ToLower())
                        continue;

                    countGrid = true;
                    string key;

                    key = FactionUtils.GetPlayerFactionTag(block.BuiltBy);

                    if (groupby != "faction")
                        key = key.PadRight(5) +" "+ PlayerUtils.GetPlayerNameById(block.BuiltBy);

                    if (!blockCounts.ContainsKey(key))
                        blockCounts.Add(key, 0);

                    blockCounts[key]++;
                }

                if (countGrid)
                    gridCount++;
            }

            List<string> myList = new List<string>(blockCounts.Keys);

            myList.Sort(delegate (string pair1, string pair2) {
                return blockCounts[pair2].CompareTo(blockCounts[pair1]);
            });

            StringBuilder sb = new StringBuilder();

            long totalValue = 0;

            foreach (string key in myList) {

                long blocks = blockCounts[key];

                sb.AppendLine(blocks.ToString("#,##0").PadRight(7) + "   " + key);

                totalValue += blocks;
            }

            sb.AppendLine();
            sb.AppendLine("Total: " + totalValue.ToString("#,##0") + " Blocks");

            sb.AppendLine();
            sb.AppendLine(gridCount.ToString("#,##0") + " Grids checked");


            if (Context.Player == null) {

                Context.Respond(title);
                Context.Respond(sb.ToString());

            } else {

                string subtitle = "Type: "+type;

                ModCommunication.SendMessageTo(new DialogMessage(title, subtitle, sb.ToString()), Context.Player.SteamUserId);
            }
        }
    }
}
