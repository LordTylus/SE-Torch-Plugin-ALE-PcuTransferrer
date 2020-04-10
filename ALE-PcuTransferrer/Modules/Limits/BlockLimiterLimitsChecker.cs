using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALE_GridManager.Modules.Limits {

    public class BlockLimiterLimitsChecker : ILimitChecker {

        public LimitCheckResponse CheckLimits(List<MySlimBlock> blocks, MyIdentity newAuthor) {

            /* BlockLimiter only checks for BlockTypes so PCU and Block Count limits are just 0 */
            var response = new LimitCheckResponse {
                CurrentPcu = 0,
                PcuLimit = 0,
                PcuAfterTransfer = 0,
                CurrentBlocks = 0,
                BlockLimit = 0,
                BlockLimitAfterTransfer = 0
            };

            /* TODO: Do the actual check */

            return response;
        }

        public string GetName() {
            return "BlockLimiter Plugin";
        }
    }
}
