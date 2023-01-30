using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch;

namespace COTHPlugin.COTHPlugin
{
    public class COTHConfig : ViewModel
    {
        private int _UPS = 1;
        private int _multiplier = 10;
        private double _percentOfWinners = 0.5;

        public int Multiplier { get => _multiplier; set => SetValue(ref _multiplier, value); }
        public int UPS { get { return _UPS; } set { SetValue(ref _UPS, value); } }
        public double PercentOfWinners { get { return _percentOfWinners; } set { SetValue(ref _percentOfWinners, value); } }
    }
}
