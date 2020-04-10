using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALE_GridManager.Modules.Limits {
    
    public interface ILimitChecker {

        LimitCheckResponse CheckLimits(List<MySlimBlock> blocks, MyIdentity newAuthor);

        string GetName();
    }
}
