using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSFLDraftCreator.Models
{
    public class PercentileChartModel
    {
        public string Pos { get; set; } = string.Empty;
        public int Per { get; set; } = 1;
        public int Str { get; set; } = 1;
        public int Agi { get; set; } = 1;
        public int Arm { get; set; } = 1;
        public int Spe { get; set; } = 1;
        public int Han { get; set; } = 1;
        public int Intel { get; set; } = 1;
        public int Acc { get; set; } = 1;
        public int PBl { get; set; } = 1;
        public int RBl { get; set; } = 1;
        public int Tck { get; set; } = 1;
        public int KDi { get; set; } = 1;
        public int KAc { get; set; } = 1;
        public int End { get; set; } = 1;
    }
}
