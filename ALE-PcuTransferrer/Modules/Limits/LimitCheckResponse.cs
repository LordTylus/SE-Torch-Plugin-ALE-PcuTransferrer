using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALE_GridManager.Modules.Limits {
    
    public class LimitCheckResponse {

        private readonly Dictionary<MyCubeBlockDefinition, int> overlimitBlocks = new Dictionary<MyCubeBlockDefinition, int>();

        public int PcuLimit { get; set; }
        public int CurrentPcu { get; set; }
        public int PcuAfterTransfer { get; set; }

        public int BlockLimit { get; set; }
        public int CurrentBlocks { get; set; }
        public int BlockLimitAfterTransfer { get; set; }

        public bool TypeLimitsFine { get { return overlimitBlocks.Count == 0; } }
        public bool PcuFine { get { return PcuLimit >= PcuAfterTransfer; } }
        public bool BlockLimitFine { get { return BlockLimit >= BlockLimitAfterTransfer; } }

        public IReadOnlyDictionary<MyCubeBlockDefinition, int> OverLimitBlocks { get { return overlimitBlocks; } }

        public void AddOverLimitBlock(MySlimBlock block) {

            var defintion = block.BlockDefinition;

            overlimitBlocks.TryGetValue(defintion, out int value);
            overlimitBlocks[defintion] = value + 1;
        }
    }
}
