using NLog;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ALE_GridManager.Modules.Limits {

    public class BlockLimiterLimitsChecker : ILimitChecker {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly MethodInfo canAddMethod;
        private readonly GridManagerConfig gridManagerConfig;

        public BlockLimiterLimitsChecker(MethodInfo canAddMethod, GridManagerConfig gridManagerConfig) {
            this.canAddMethod = canAddMethod;
            this.gridManagerConfig = gridManagerConfig;
        }

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

            if (!gridManagerConfig.UseBlockLimiter)
                return response;

            try {

                object[] parameters = new object[] { blocks, newAuthor.IdentityId, null };

                canAddMethod.Invoke(null, parameters);

                List<MySlimBlock> notAllowedBlocks = (List<MySlimBlock>)parameters[2];

                if(notAllowedBlocks != null)
                    foreach (MySlimBlock block in notAllowedBlocks)
                        response.AddOverLimitBlock(block);

            } catch (Exception e) {
                Log.Warn(e, "BlockLimiter was unable to verify the transfer request. It is therefore ignored!");
            }

            return response;
        }

        public string GetName() {
            return "BlockLimiter Plugin";
        }
    }
}
