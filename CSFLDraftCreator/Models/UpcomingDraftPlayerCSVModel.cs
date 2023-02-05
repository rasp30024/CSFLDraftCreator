
using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSFLDraftCreator.Models
{
    internal class UpcomingDraftPlayerCSVModel
    {
        public int Id { get; set; }
        public string RefId { get; set; }
        public int Sea { get; set; }
        public string First { get; set; }
        public string Last { get; set; }
        public string Team { get; set; }
        public string Coll { get; set; }
        public int Num { get; set; }
        public int Age { get; set; }
        public int Hgt { get; set; }
        public int Wgt { get; set; }
        public string Pos { get; set; }

        //PlayerAttributesModel 
        public int Str { get; set; }
        public int Agi { get; set; }
        public int Arm { get; set; }
        public int Spe { get; set; }
        public int Han { get; set; }
        public int Intel { get; set; }
        public int Acc { get; set; }
        public int PBl { get; set; }
        public int RBl { get; set; }
        public int Tck { get; set; }
        public int KDi { get; set; }
        public int KAc { get; set; }
        public int End { get; set; }

        //PlayerPersonalitiesModel Object
        public int Lea { get; set; }
        public int Wor { get; set; }
        public int Com { get; set; }
        public int TmPl { get; set; }
        public int Spor { get; set; }
        public int Soc { get; set; }
        public int Mny { get; set; }
        public int Sec { get; set; }
        public int Loy { get; set; }
        public int Win { get; set; }
        public int PT { get; set; }
        public int Home { get; set; }
        public int Mkt { get; set; }
        public int Mor { get; set; }

        //PlayerSkillsModel Object
        public int QB { get; set; }
        public int RB { get; set; }
        public int FB { get; set; }
        public int WR { get; set; }
        public int TE { get; set; }
        public int G { get; set; }
        public int T { get; set; }
        public int C { get; set; }
        public int P { get; set; }
        public int K { get; set; }
        public int DT { get; set; }
        public int DE { get; set; }
        public int LB { get; set; }
        public int CB { get; set; }
        public int SS { get; set; }
        public int FS { get; set; }

        //normal file
        public string Flg { get; set; }
        [Optional]
        public string Trait { get; set; }
    }
}

    