using ALE_GridManager;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;

namespace ALE_PcuTransferrer.Commands {
    public class StatisticCommands : CommandModule {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public GridManagerPlugin Plugin => (GridManagerPlugin)Context.Plugin;



    }
}
