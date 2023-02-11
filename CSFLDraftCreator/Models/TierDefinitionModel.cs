using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSFLDraftCreator.Models
{
    internal class TierDefinitionModel
    {
        public string Id { get; set; }
        public int Order { get; set; }
        public int KeyMax { get; set; }
        public int KeyMin { get; set; }
        public int SecMax { get; set; }
        public int SecMin { get; set; }
        public int Skill { get; set; }
        public int WE { get; set; }
        public int End { get; set; } 
        public bool AllowTag { get; set; }
    }
}
