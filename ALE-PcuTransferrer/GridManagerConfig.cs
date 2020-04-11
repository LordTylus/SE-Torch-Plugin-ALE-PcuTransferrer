using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch;

namespace ALE_GridManager {
    
    public class GridManagerConfig : ViewModel {

        private bool _useBlockLimiter = false;

        public bool UseBlockLimiter { get => _useBlockLimiter; set => SetValue(ref _useBlockLimiter, value); }
    }
}
