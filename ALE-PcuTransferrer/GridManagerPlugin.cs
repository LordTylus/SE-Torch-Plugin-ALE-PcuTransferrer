using NLog;
using System.Collections.Concurrent;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using System.Collections.Generic;
using System.Windows.Controls;
using ALE_GridManager.UI;
using ALE_GridManager.Modules;
using ALE_GridManager.Modules.Limits;
using Torch.Session;
using Torch.Managers;
using System;
using System.Reflection;
using System.IO;
using ALE_Core.Cooldown;

namespace ALE_GridManager {

    public class GridManagerPlugin : TorchPluginBase, IWpfPlugin {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly Guid BLOCK_LIMITER_UUID = new Guid("11fca5c4-01b6-4fc3-a215-602e2325be2b");

        private UserControl _control;

        public UserControl GetControl() => _control ?? (_control = new CommandsUi(this));

        public CooldownManager CooldownManager { get; } = new CooldownManager();

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

        private Persistent<GridManagerConfig> _config;
        public GridManagerConfig Config => _config?.Data;

        public void Save() => _config.Save();

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

            SetupConfig();

            var sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
                sessionManager.SessionStateChanged += SessionChanged;
            else
                Log.Warn("No session manager loaded!");
        }

        private void SetupConfig() {

            var configFile = Path.Combine(StoragePath, "GridManager.cfg");

            try {

                _config = Persistent<GridManagerConfig>.Load(configFile);

            } catch (Exception e) {
                Log.Warn(e);
            }

            if (_config?.Data == null) {

                Log.Info("Create Default Config, because none was found!");

                _config = new Persistent<GridManagerConfig>(configFile, new GridManagerConfig());
                _config.Save();
            }
        }

        private void SessionChanged(ITorchSession session, TorchSessionState newState) {

            /*
             * After Session is Started we add the BlockLimiter plugin if detected.
             * We do it here as we cannot be sure if it is installed at all when checking earlier,
             * because it may not be loaded in yet.
             */
            if(newState == TorchSessionState.Loaded) {

                var pluginManager = Torch.Managers.GetManager<PluginManager>();
                
                if(pluginManager.Plugins.TryGetValue(BLOCK_LIMITER_UUID, out ITorchPlugin blockLimiterPlugin)) {

                    try {

                        MethodInfo canAddMethod = blockLimiterPlugin.GetType().GetMethod("CanAdd", BindingFlags.Static | BindingFlags.Public);

                        _groupCheckModule.AddLimitChecker(new BlockLimiterLimitsChecker(canAddMethod, Config));

                        Log.Info("BlockLimiter Reference added to PCU-Transferrer for limit checks.");
                    
                    } catch (Exception e) {
                        Log.Warn(e, "Could not connect to Blocklimiter Plugin. Make sure to let the PCU-Transferrer developer about it!");
                    }

                } else {
                    Log.Info("BlockLimiter Plugin not found! Uses vanilla Limits only!");
                }
            }
        }
    }
}