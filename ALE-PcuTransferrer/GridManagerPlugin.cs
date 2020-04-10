using NLog;
using System.Collections.Concurrent;
using Torch;
using Torch.API;
using System.Collections.Generic;
using System.Windows.Controls;
using Torch.API.Plugins;
using ALE_GridManager.UI;
using ALE_Core;
using ALE_GridManager.Modules;
using ALE_GridManager.Modules.Limits;

namespace ALE_GridManager {

    public class GridManagerPlugin : TorchPluginBase, IWpfPlugin {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private UserControl _control;
        public UserControl GetControl() => _control ?? (_control = new CommandsUi());

        public Dictionary<long, CurrentCooldown> ConfirmationsMap { get; } = new Dictionary<long, CurrentCooldown>();
        public ConcurrentDictionary<long, long> PlayersOnFreebuild { get; } = new ConcurrentDictionary<long, long>();

        public long CooldownConfirmation { get { return 30 * 1000; } }

        private readonly GroupCheckModule _groupCheckModule;
        private readonly RepairModule _repairModule;
        private readonly TransferModule _transferModule;
        private readonly CheckModule _checkModule;

        public GroupCheckModule GroupCheckModule { get { return _groupCheckModule; } }
        public RepairModule RepairModule { get { return _repairModule; } }
        public TransferModule TransferModule { get { return _transferModule; } }
        public CheckModule CheckModule { get { return _checkModule; } }

        public GridManagerPlugin() {

            _groupCheckModule = new GroupCheckModule();
            _groupCheckModule.AddLimitChecker(new VanillaLimitsChecker());

            _repairModule = new RepairModule(_groupCheckModule);
            _transferModule = new TransferModule(_groupCheckModule);
            _checkModule = new CheckModule(_groupCheckModule);
        }

        /// <inheritdoc />
        public override void Init(ITorchBase torch) {
            
            base.Init(torch);

        }
    }
}