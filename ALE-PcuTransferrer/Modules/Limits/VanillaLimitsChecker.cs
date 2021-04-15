using ALE_Core.Utils;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using static Sandbox.Game.World.MyBlockLimits;

namespace ALE_GridManager.Modules.Limits {
    
    public class VanillaLimitsChecker : ILimitChecker {

        public LimitCheckResponse CheckLimits(List<MySlimBlock> blocks, MyIdentity newAuthor) {

            var response = new LimitCheckResponse();

            bool blockLimitsEnabled = MySession.Static.BlockLimitsEnabled != MyBlockLimitsEnabledEnum.NONE;

            if (!blockLimitsEnabled)
                return response;

            Dictionary<string, short> vanillaLimits = MySession.Static.BlockTypeLimits;
            Dictionary<string, short> limits = new Dictionary<string, short>(vanillaLimits);

            var blockLimits = newAuthor.BlockLimits;

            response.CurrentPcu = blockLimits.PCUBuilt;
            response.PcuLimit = blockLimits.PCU + blockLimits.PCUBuilt;
            response.CurrentBlocks = blockLimits.BlocksBuilt;
            response.BlockLimit = blockLimits.MaxBlocks;

            foreach (string blockType in blockLimits.BlockTypeBuilt.Keys) {

                MyTypeLimitData limit = blockLimits.BlockTypeBuilt[blockType];

                if (!limits.ContainsKey(blockType))
                    continue;

                short remainingBlocks = (short)(limits[blockType] - limit.BlocksBuilt);

                limits.Remove(blockType);
                limits.Add(blockType, remainingBlocks);
            }

            int pcusOfGroup = 0;
            int blockCountOfGroup = 0;

            foreach (MySlimBlock block in blocks) {

                pcusOfGroup += BlockUtils.GetPcu(block);
                blockCountOfGroup++;

                string blockType = block.BlockDefinition.BlockPairName;

                if (!limits.ContainsKey(blockType))
                    continue;

                short remainingBlocks = (short)(limits[blockType] - 1);

                if (remainingBlocks < 0)
                    response.AddOverLimitBlock(block);

                limits[blockType] = remainingBlocks;
            }

            /* If Block Limit is not Disabled check for that. */
            if (response.BlockLimit > 0)
                response.BlockLimitAfterTransfer = response.CurrentBlocks + blockCountOfGroup;
            
            /* If PCU Limit is not Disabled check for that. */
            if(response.PcuLimit > 0)
                response.PcuAfterTransfer = response.CurrentPcu + pcusOfGroup;

            return response;
        }

        public string GetName() {
            return "Vanilla Limits";
        }
    }
}
