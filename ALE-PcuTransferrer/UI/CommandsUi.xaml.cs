using System.Windows;
using System.Windows.Controls;

namespace ALE_GridManager.UI {

    public partial class CommandsUi : UserControl {

        private GridManagerPlugin Plugin { get; }

        private CommandsUi() {
            InitializeComponent();
        }

        public CommandsUi(GridManagerPlugin plugin) : this() {
            Plugin = plugin;
            DataContext = plugin.Config;
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e) {
            Plugin.Save();
        }
    }
}