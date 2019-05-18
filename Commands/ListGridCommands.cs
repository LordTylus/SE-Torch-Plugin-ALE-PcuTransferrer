using ALE_GridManager;
using ALE_PcuTransferrer.Utils;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Text;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace ALE_PcuTransferrer.Commands {
    public class ListGridCommands : CommandModule {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public GridManagerPlugin Plugin => (GridManagerPlugin)Context.Plugin;

        [Command("listgridsowner", "Checks which grids the given player has ownership on.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void InspectOwner() {

            string playerName = null;
            bool gps = false;
            bool position = false;

            List<string> args = Context.Args;

            if (args.Count == 1) {

                playerName = args[0];

            } else if (args.Count > 1) {

                playerName = args[0];

                for (int i = 1; i < args.Count; i++) {

                    if (args[i] == "-gps")
                        gps = true;

                    if (args[i] == "-position")
                        position = true;
                }

            } else {

                Context.Respond("Correct Usage is !listgridsowner <playerName> [-gps] [-position]");
                return;
            }

            IMyIdentity identity = PlayerUtils.GetIdentityByName(playerName);

            if(identity == null) {

                Context.Respond("Player not found!");
                return;
            }

            long id = identity.IdentityId;

            StringBuilder sb = new StringBuilder();

            List<MyCubeGrid> bigOwnerGrids = new List<MyCubeGrid>();
            List<MyCubeGrid> smallOwnerGrids = new List<MyCubeGrid>();

            foreach (MyEntity entity in MyEntities.GetEntities()) {

                MyCubeGrid grid = entity as MyCubeGrid;

                if (grid == null)
                    continue;

                if (grid.Physics == null)
                    continue;

                if (grid.BigOwners.Contains(id))
                    bigOwnerGrids.Add(grid);
                else if (grid.SmallOwners.Contains(id))
                    smallOwnerGrids.Add(grid);
            }

            sb.AppendLine("More then 50 % Ownership");
            sb.AppendLine("---------------------------------------");

            foreach (MyCubeGrid grid in bigOwnerGrids)
                AddGridToSb(grid, sb, position, gps, id, false);

            sb.AppendLine("");
            sb.AppendLine("Less then 50 % Ownership");
            sb.AppendLine("---------------------------------------");

            foreach (MyCubeGrid grid in smallOwnerGrids)
                AddGridToSb(grid, sb, position, gps, id, false);

            if (Context.Player == null) {

                Context.Respond($"Grids owned by {playerName}");
                Context.Respond(sb.ToString());

            } else {

                ModCommunication.SendMessageTo(new DialogMessage("List Grids Ownership", $"Grids owned by {playerName}", sb.ToString()), Context.Player.SteamUserId);
            }
        }

        [Command("listgridsauthor", "Checks which grids the given player has authorship on.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void InspectAuthor() {

            string playerName = null;
            bool gps = false;
            bool position = false;

            List<string> args = Context.Args;

            if (args.Count == 1) {

                playerName = args[0];

            } else if (args.Count > 1) {

                playerName = args[0];

                for (int i = 1; i < args.Count; i++) {

                    if (args[i] == "-gps")
                        gps = true;

                    if (args[i] == "-position")
                        position = true;
                }

            } else {

                Context.Respond("Correct Usage is !listgridsauthor <playerName> [-gps] [-position]");
                return;
            }

            IMyIdentity identity = PlayerUtils.GetIdentityByName(playerName);

            if (identity == null) {

                Context.Respond("Player not found!");
                return;
            }

            long id = identity.IdentityId;

            StringBuilder sb = new StringBuilder();

            foreach (MyEntity entity in MyEntities.GetEntities()) {

                MyCubeGrid grid = entity as MyCubeGrid;

                if (grid == null)
                    continue;

                if (grid.Physics == null)
                    continue;

                AddGridToSb(grid, sb, position, gps, id, true);
            }

            if (Context.Player == null) {

                Context.Respond($"Grids built by {playerName}");
                Context.Respond(sb.ToString());

            } else {

                ModCommunication.SendMessageTo(new DialogMessage("List Grids Authorships", $"Grids built by {playerName}", sb.ToString()), Context.Player.SteamUserId);
            }
        }

        private void AddGridToSb(MyCubeGrid grid, StringBuilder sb, bool showPosition, bool showGps, long playerId, bool pcu) {

            HashSet<MySlimBlock> blocks = new HashSet<MySlimBlock>(grid.GetBlocks());

            int value = 0;

            foreach (MySlimBlock block in blocks) {

                if (block == null || block.CubeGrid == null || block.IsDestroyed)
                    continue;

                if (!pcu) {

                    MyCubeBlock cubeBlock = block.FatBlock;
                    if (cubeBlock != null && cubeBlock.OwnerId == playerId)
                        value++;

                    continue;
                }

                if (block.BuiltBy == playerId) {

                    int pcuValue = 1;
                    if (block.ComponentStack.IsFunctional)
                        pcuValue = block.BlockDefinition.PCU;

                    value += pcuValue;
                }
            }

            if (value == 0)
                return;

            if (pcu)
                sb.AppendLine($"{grid.DisplayName} - {value} PCU");
            else
                sb.AppendLine($"{grid.DisplayName} - {value} blocks");

            if(showPosition) { 

                var position = grid.PositionComp.GetPosition();

                sb.AppendLine($"   X: {position.X.ToString("#,##0.00")}, Y: {position.Y.ToString("#,##0.00")}, Z: {position.Z.ToString("#,##0.00")}");
            }

            if (showGps && Context.Player != null) {

                var gridGPS = MyAPIGateway.Session?.GPS.Create("--"+grid.DisplayName, ($"{grid.DisplayName} - {grid.GridSizeEnum} - {grid.BlocksCount} blocks"), grid.PositionComp.GetPosition(), true);

                MyAPIGateway.Session?.GPS.AddGps(Context.Player.IdentityId, gridGPS);
            }
        }
    }
}
