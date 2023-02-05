using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSFLDraftCreator.Models
{
    internal class SearchPlayerExportCSVModel
    {
        public string Team { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Pos { get; set; } = string.Empty;
        public int Str { get; set; } = 0;
        public int Agi { get; set; } = 0;
        public int Arm { get; set; } = 0;
        public int Int { get; set; } = 0;
        public int Acc { get; set; } = 0;
        public int Tck { get; set; } = 0;
        public int Spe { get; set; } = 0;
        public int Hnd { get; set; } = 0;
        public int PBl { get; set; } = 0;
        public int RBl { get; set; } = 0;
        public int KDi { get; set; } = 0;
        public int KAc { get; set; } = 0;
        public int End { get; set; } = 0;


    }
}
