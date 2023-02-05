using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSFLDraftCreator.Models
{
    internal class PlayerModel
    {
        public int Id { get; set; } = 0;
        public int Sea { get; set; } = 0;
        public int Rook { get; set; } = 0;
        public string First { get; set; } = string.Empty;
        public string Last { get; set; } = string.Empty;
        public string Team { get; set; } = string.Empty;
        public int DRnd { get; set; } = 0;
        public int DPick { get; set; } = 0;
        public int DSea { get; set; } = 0;
        public string Coll { get; set; } = string.Empty;
        public int Num { get; set; } = 0;
        public int Exp { get; set; } = 0;
        public int Age { get; set; } = 0;
        public int Hgt { get; set; } = 0;
        public int Wgt { get; set; } = 0;
        public string Pos { get; set; } = string.Empty;
        public int Yrs { get; set; } = 0;
        public PlayerAttributesModel Attr { get; set; } = new PlayerAttributesModel();
        public PlayerPersonalitiesModel Per { get; set; } = new PlayerPersonalitiesModel();
        public PlayerSkillsModel Skills { get; set; } = new PlayerSkillsModel();
        public string Flg { get; set; } = string.Empty;
        public string Faceset { get; set; } = string.Empty;
        public string Face { get; set; } = string.Empty;
        public string Hair { get; set; } = string.Empty;
        public float Sal { get; set; } = 0;
        public string Trait { get; set; } = string.Empty;
    }
}
