using ALE_Core.Utils;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.IO;
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
                string metric = "author";
                string findby = "blockpair";
                string export = null;

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

                    if (args[i].StartsWith("-metric="))
                        metric = args[i].Replace("-metric=", "");

                    if (args[i].StartsWith("-findby="))
                        findby = args[i].Replace("-findby=", "");

                    if (args[i].StartsWith("-export="))
                        export = args[i].Replace("-export=", "");
                }

                if (export != null) {

                    export = export.Trim();

                    if (export == "") {
                        Context.Respond("Invalid name for Export file, Export is ignored!");
                        export = null;
                    }
                }

                if (findby != "blockpair" && findby != "type" && findby != "subtype") {
                    Context.Respond("You can only look up blocks by type, subtype or blockpair! Will use blockpair as default.");
                    findby = "blockpair";
                }

                if (metric != "author" && metric != "owner") {
                    Context.Respond("You can only look up blocks by owner or author! Will use author as default.");
                    metric = "author";
                }

                if (orderby != "blocks" && orderby != "pcu" && orderby != "name") {
                    Context.Respond("You can only order by 'pcu', 'name' or 'blocks'! Will use blocks as default.");
                    orderby = "blocks";
                }

                if (type == "all") 
                    ListBlocks(false, countPcu, factionTag, playerName, gridName, orderby, metric, findby, export);
                else if (type == "limited") 
                    ListBlocks(true, countPcu, factionTag, playerName, gridName, orderby, metric, findby, export);
                else 
                    Context.Respond("Known type only 'all' and 'limited' is supported!");

            } else {

                Context.Respond("Correct Usage is !listblocks <all|limited> [-pcu] [-player=<playerName>] [-faction=<factionTag>] [-orderby=<pcu|name|blocks>] [-metric=<author|owner>] [-findby=<blockpair|type|subtype>] [-export=<name>]");
            }
        }

        private void ListBlocks(bool limitedOnly, bool countPCU, string factionTag, string playerName, string gridName, string orderby, string metric, string findby, string export) {

            if (limitedOnly)
                findby = "blockpair";

            Dictionary<string, short> globalLimits = Context.Torch.CurrentSession.KeenSession.BlockTypeLimits;
            Dictionary<string, long> blockCounts = new Dictionary<string, long>();
            Dictionary<string, long> pcuCounts = new Dictionary<string, long>();

            int gridCount = 0;

            HashSet<long> identities = null;

            string title = "Blocks in World";

            if (playerName != null) {

                MyIdentity player = PlayerUtils.GetIdentityByNameOrId(playerName);
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

                if (!GridUtils.MatchesGridNameOrIdWithWildcard(grid, gridName))
                    continue;

                HashSet<MySlimBlock> blocks = new HashSet<MySlimBlock>(grid.GetBlocks());

                bool countGrid = false;

                foreach (MySlimBlock block in blocks) {

                    if (block == null || block.CubeGrid == null || block.IsDestroyed)
                        continue;

                    if (identities != null && !IsMatchesIdentities(identities, block, metric)) 
                        continue;

                    countGrid = true;

                    string pairName = GetPairName(block, findby);

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
            StringBuilder exportSb = new StringBuilder();

            if(limitedOnly) 
                exportSb.AppendLine("Block Name;Block Count;Block Limit;PCU Count");
            else
                exportSb.AppendLine("Block Name;Block Count;PCU Count");

            bool isExport = export != null;

            long totalPCU = 0;
            long totalValue = 0;

            foreach (string pair in myList) {

                string pcuString = "";
                long pcu = 0L; 

                if (countPCU) {

                    pcu = pcuCounts[pair];

                    pcuString = " " + pcu.ToString("#,##0") + " PCU";

                    totalPCU += pcu;
                }

                long blocks = blockCounts[pair];

                if (limitedOnly) {

                    sb.AppendLine(blocks.ToString("#,##0").PadRight(7) + " / (" + globalLimits[pair] + ")   " + pair + pcuString);

                    if (isExport)
                        exportSb.AppendLine(pair + ";" + blocks + ";" + globalLimits[pair] + ";" + pcu);

                } else {

                    sb.AppendLine(blocks.ToString("#,##0").PadRight(7) + "   " + pair + pcuString);

                    if (isExport)
                        exportSb.AppendLine(pair + ";" + blocks + ";" + pcu);
                }

                totalValue += blocks;
            }

            sb.AppendLine();
            sb.AppendLine("Total: " + totalValue.ToString("#,##0") + " Blocks");

            if (countPCU)
                sb.AppendLine("Total: " + totalPCU.ToString("#,##0") + " PCU");

            sb.AppendLine();
            sb.AppendLine(gridCount.ToString("#,##0") + " Grids checked");

            if(isExport) {

                ExportToFile(exportSb, export);

                return;
            }

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

        [Command("listallblocks", "Lists how many of which Blocks each player in your world has.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void ListAllBlocks() {

            List<string> args = Context.Args;

            string export = null;
            string metric = "author";
            string findby = "blockpair";

            for (int i = 0; i < args.Count; i++) {

                if (args[i].StartsWith("-export="))
                    export = args[i].Replace("-export=", "");

                if (args[i].StartsWith("-metric="))
                    metric = args[i].Replace("-metric=", "");

                if (args[i].StartsWith("-findby="))
                    findby = args[i].Replace("-findby=", "");
            }

            if (findby != "blockpair" && findby != "type" && findby != "subtype") {
                Context.Respond("You can only look up blocks by type, subtype or blockpair! Will use blockpair as default.");
                findby = "blockpair";
            }

            if (metric != "author" && metric != "owner") {
                Context.Respond("You can only look up blocks by owner or author! Will use author as default.");
                metric = "author";
            }

            if (export != null) {

                export = export.Trim();

                if (export == "") {
                    Context.Respond("Invalid name for Export file, Export is ignored!");
                    export = null;
                }
            }

            ListAllBlocks(metric, export, findby);
        }

        private void ListAllBlocks(string metric, string export, string findby) {

            Dictionary<ListKey, Dictionary<string, BlockInfo>> blockCounts = new Dictionary<ListKey, Dictionary<string, BlockInfo>>();

            int gridCount = 0;
            long blockCount = 0;
            long pcuCount = 0;

            foreach (MyEntity entity in MyEntities.GetEntities()) {

                if (!(entity is MyCubeGrid grid))
                    continue;

                if (grid.Physics == null)
                    continue;

                HashSet<MySlimBlock> blocks = new HashSet<MySlimBlock>(grid.GetBlocks());

                foreach (MySlimBlock block in blocks) {

                    if (block == null || block.CubeGrid == null || block.IsDestroyed)
                        continue;

                    long ownerId;

                    if (metric == "author")
                        ownerId = block.BuiltBy;
                    else
                        ownerId = block.OwnerId;

                    ListKey key = new ListKey {
                        Id = ownerId
                    };

                    Dictionary<string, BlockInfo> infoDict;

                    if (!blockCounts.ContainsKey(key)) {

                        /* Only check that one once */
                        key.Faction = FactionUtils.GetPlayerFactionTag(ownerId);
                        key.Name = PlayerUtils.GetPlayerNameById(ownerId);

                        infoDict = new Dictionary<string, BlockInfo>();

                        blockCounts.Add(key, infoDict);

                    } else {

                        infoDict = blockCounts[key];
                    }

                    string pairName = GetPairName(block, findby);
                    BlockInfo info;

                    if (!infoDict.ContainsKey(pairName)) {

                        info = new BlockInfo {
                            PairName = pairName
                        };

                        infoDict.Add(pairName, info);
                    
                    } else {
                        info = infoDict[pairName];
                    }

                    int pcu = BlockUtils.GetPcu(block);

                    info.PCU += pcu;
                    info.Count++;

                    pcuCount += pcu;
                    blockCount++;
                }

                gridCount++;
            }

            var myList = new List<ListKey>(blockCounts.Keys);

            myList.Sort(delegate (ListKey pair1, ListKey pair2) {
                return pair1.Name.CompareTo(pair2.Name);
            });

            string title = "Blocks per Player";

            StringBuilder sb = new StringBuilder();
            StringBuilder exportSb = new StringBuilder();

            exportSb.AppendLine("Id;Name;Faction;Block Name;Block Count;PCU Count");

            bool isExport = export != null;

            foreach (ListKey key in myList) {

                Dictionary<string, BlockInfo> infoDict = blockCounts[key];

                List<string> pairnames = new List<string>(infoDict.Keys);

                pairnames.Sort(delegate (string pair1, string pair2) {
                    return pair1.CompareTo(pair2);
                });

                sb.AppendLine(key.Name + " [" + key.Faction + "]");
                sb.AppendLine("-------------------------------------");

                foreach (string pairname in pairnames) {

                    BlockInfo info = infoDict[pairname];

                    sb.AppendLine("   "+info.Count.ToString("#,##0").PadRight(5) + "   " + info.PairName+" - "+info.PCU+" PCU");

                    if (isExport)
                        exportSb.AppendLine(key.Id + ";" + key.Name + ";" + key.Faction + ";" + info.PairName + ";" + info.Count + ";" + info.PCU);
                }

                sb.AppendLine("");
            }

            sb.AppendLine();
            sb.AppendLine("Total: " + blockCount.ToString("#,##0") + " Blocks");
            sb.AppendLine("Total: " + pcuCount.ToString("#,##0") + " PCU");

            sb.AppendLine();
            sb.AppendLine(gridCount.ToString("#,##0") + " Grids checked");

            if (isExport) {

                ExportToFile(exportSb, export);

                return;
            }

            if (Context.Player == null) {

                Context.Respond(title);
                Context.Respond(sb.ToString());

            } else {

                string subtitle = "All Blocks";

                ModCommunication.SendMessageTo(new DialogMessage(title, subtitle, sb.ToString()), Context.Player.SteamUserId);
            }
        }

        private bool IsMatchesIdentities(HashSet<long> identities, MySlimBlock block, string metric) {

            if (metric == "author" && !identities.Contains(block.BuiltBy))
                return false;

            if (metric == "owner" && !identities.Contains(block.OwnerId))
                return false;

            return true;
        }

        private bool IsMatchesNobody(MySlimBlock block, string metric) {

            if (metric == "author" && block.BuiltBy != 0)
                return false;

            if (metric == "owner" && block.OwnerId != 0)
                return false;

            return true;
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
                string metric = "author";
                string findby = "blockpair";
                string export = null;
                bool nobody = false;

                for (int i = 1; i < args.Count; i++) {

                    if (args[i] == "-nobody")
                        nobody = true;

                    if (args[i].StartsWith("-faction="))
                        factionTag = args[i].Replace("-faction=", "");

                    if (args[i].StartsWith("-player="))
                        playerName = args[i].Replace("-player=", "");

                    if (args[i].StartsWith("-groupby="))
                        groupby = args[i].Replace("-groupby=", "");

                    if (args[i].StartsWith("-metric="))
                        metric = args[i].Replace("-metric=", "");

                    if (args[i].StartsWith("-findby="))
                        findby = args[i].Replace("-findby=", "");

                    if (args[i].StartsWith("-export="))
                        export = args[i].Replace("-export=", "");
                }

                if (export != null) {
                    
                    export = export.Trim();

                    if (export == "") {
                        Context.Respond("Invalid name for Export file, Export is ignored!");
                        export = null;
                    }
                }

                if (findby != "blockpair" && findby != "type" && findby != "subtype") {
                    Context.Respond("You can only look up blocks by type, subtype or blockpair! Will use blockpair as default.");
                    findby = "blockpair";
                }

                if (metric != "author" && metric != "owner") {
                    Context.Respond("You can only look up blocks by owner or author! Will use author as default.");
                    metric = "author";
                }

                if (groupby != "player" && groupby != "faction" && groupby != "grid") {
                    Context.Respond("You can only group by 'player', 'faction' and 'grid'! Will use player as default.");
                    groupby = "player";
                }

                FindBlocks(type, factionTag, playerName, groupby, metric, findby, export, nobody);

            } else {

                Context.Respond("Correct Usage is !findblock <blockpairname> [-player=<playerName>] [-faction=<factionTag>] [-groupby=<player|faction>] [-metric=<author|owner>] [-findby=<blockpair|type|subtype>]");
            }
        }

        private void FindBlocks(string type, string factionTag, string playerName, string groupby, string metric, string findby, string export, bool nobody) {

            Dictionary<ListKey, long> blockCounts = new Dictionary<ListKey, long>();

            int gridCount = 0;

            HashSet<long> identities = null;

            string title = "Blocks in World";

            if (!nobody) {

                if (playerName != null) {

                    MyIdentity player = PlayerUtils.GetIdentityByNameOrId(playerName);
                    if (player == null) {

                        Context.Respond("Player not found!");
                        return;
                    }

                    title = "Blocks of Player " + playerName;

                    identities = new HashSet<long> {
                    player.IdentityId
                };

                } else if (factionTag != null) {

                    IMyFaction faction = FactionUtils.GetIdentityByTag(factionTag);

                    if (faction == null) {

                        Context.Respond("Faction not found!");
                        return;
                    }

                    title = "Blocks of Faction " + factionTag;

                    identities = new HashSet<long>();
                    foreach (long identityId in faction.Members.Keys)
                        identities.Add(identityId);
                }
            
            } else {

                title = "Blocks of Nobody";
            }

            foreach (MyEntity entity in MyEntities.GetEntities()) {
                
                if (!(entity is MyCubeGrid grid))
                    continue;

                if (grid.Physics == null)
                    continue;

                HashSet<MySlimBlock> blocks = new HashSet<MySlimBlock>(grid.GetBlocks());

                bool countGrid = false;

                long gridowner = OwnershipUtils.GetOwner(grid);
                string ownerName = PlayerUtils.GetPlayerNameById(gridowner);
                
                string ownerFactionTag = FactionUtils.GetPlayerFactionTag(gridowner);

                if (ownerFactionTag != "")
                    ownerFactionTag = " [" + ownerFactionTag + "]";

                foreach (MySlimBlock block in blocks) {

                    if (block == null || block.CubeGrid == null || block.IsDestroyed)
                        continue;

                    if (identities != null && !IsMatchesIdentities(identities, block, metric))
                        continue;

                    if (nobody && !IsMatchesNobody(block, metric))
                        continue;

                    if (!MatchesType(block, findby, type))
                        continue;

                    countGrid = true;

                    long ownerId = block.BuiltBy;

                    if (metric == "owner")
                        ownerId = block.OwnerId;

                    var faction = MySession.Static.Factions.TryGetPlayerFaction(ownerId);
                    string factionName = "[no Faction]";
                    string blockFaction = "";
                    long factionId = 0;

                    if (faction != null) {
                        blockFaction = faction.Tag;
                        factionId = faction.FactionId;
                        factionName = faction.Name;
                    }

                    ListKey key = new ListKey {
                        Faction = blockFaction
                    };

                    if (groupby == "faction") {
                        key.Name = factionName;
                        key.Id = factionId;
                    }

                    if (groupby == "player") {
                        key.Name = PlayerUtils.GetPlayerNameById(ownerId);
                        key.Id = ownerId;
                    }

                    if (groupby == "grid") {
                        key.Name = grid.DisplayName;
                        key.Id = grid.EntityId;
                        key.IsGrid = true;
                        key.OwnerName = ownerName;
                    }

                    if (!blockCounts.ContainsKey(key))
                        blockCounts.Add(key, 0);

                    blockCounts[key]++;
                }

                if (countGrid)
                    gridCount++;
            }

            List<ListKey> myList = new List<ListKey>(blockCounts.Keys);

            myList.Sort(delegate (ListKey pair1, ListKey pair2) {
                return blockCounts[pair2].CompareTo(blockCounts[pair1]);
            });

            StringBuilder sb = new StringBuilder();
            StringBuilder exportSb = new StringBuilder();

            exportSb.AppendLine("Id;Name;Faction;Owner;Block Count");

            bool isExport = export != null;

            long totalValue = 0;

            foreach (ListKey key in myList) {

                long blocks = blockCounts[key];

                if(!key.IsGrid)
                    sb.AppendLine(blocks.ToString("#,##0").PadRight(7) + "   " + key.Faction.PadRight(5) + key.Name);
                else
                    sb.AppendLine(blocks.ToString("#,##0").PadRight(7) + "   " + key.Id + " " + key.Name + " - Owned by: " + key.OwnerName + " " + key.Faction);

                if (isExport)
                    exportSb.AppendLine(key.Id + ";" + key.Name + ";" + key.Faction + ";" + key.OwnerName + ";" + blocks);

                totalValue += blocks;
            }

            sb.AppendLine();
            sb.AppendLine("Total: " + totalValue.ToString("#,##0") + " Blocks");

            sb.AppendLine();
            sb.AppendLine(gridCount.ToString("#,##0") + " Grids checked");

            if (isExport) {

                ExportToFile(exportSb, export);

                return;
            }

            if (Context.Player == null) {

                Context.Respond(title);
                Context.Respond(sb.ToString());

            } else {

                string subtitle = "Type: "+type;

                ModCommunication.SendMessageTo(new DialogMessage(title, subtitle, sb.ToString()), Context.Player.SteamUserId);
            }
        }

        private void ExportToFile(StringBuilder exportSb, string export) {

            string path = CreatePath(export);

            File.WriteAllText(path, exportSb.ToString());

            Context.Respond("Exported to '" + path + "'");
        }

        public string CreatePath(string export) {

            foreach (var c in Path.GetInvalidFileNameChars())
                export = export.Replace(c, '_');

            var folder = Path.Combine(Plugin.StoragePath, "ExportedStatistics");
            Directory.CreateDirectory(folder);

            return Path.Combine(folder, export + ".csv");
        }

        private bool MatchesType(MySlimBlock block, string findby, string type) {

            string pairName = GetPairName(block, findby);

            if (pairName == null || pairName.ToLower() != type.ToLower())
                return false;

            return true;
        }

        private string GetPairName(MySlimBlock block, string findby) {

            var blockDefinition = block.BlockDefinition;

            if (findby == "type") {
                
                string typeString = blockDefinition.Id.TypeId.ToString();

                return typeString.Replace("MyObjectBuilder_", "");
            }

            if(findby == "subtype")
                return blockDefinition.Id.SubtypeId.ToString();

            return blockDefinition.BlockPairName;
        }

        private class BlockInfo {

            public string PairName { get; set; }
            public long Count { get; set; }
            public long PCU { get; set; }
        }

        private class ListKey {

            public string Name { get; set; }
            public string Faction { get; set; }
            public long Id { get; set; }

            public bool IsGrid { get; set; }

            public string OwnerName { get; set; }

            public override bool Equals(object obj) {
                return obj is ListKey key &&
                        Id == key.Id;
            }

            public override int GetHashCode() {
                var hashCode = -1776534320;
                hashCode = hashCode * -1521134295 + Id.GetHashCode();
                return hashCode;
            }
        }
    }
}
