using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSFLDraftCreator.ConfigModels
{
    public class TierModel
    {
        [Name("TierName")] 
        public string TierName { get; set; }
        
        [Name("Order")]
        public int Order { get; set; }
        
        [Name("KeyAttributeMin")]
        public int KeyAttributeMin { get; set; }
        
        [Name("KeyAttributeMax")]
        public int KeyAttributeMax { get; set; }
        
        [Name("PrimaryAttributeMin")]
        public int PriAttributeMin { get; set; }
        
        [Name("PrimaryAttributeMax")]
        public int PriAttributeMax { get; set; }
        
        [Name("SecondaryAttributeMin")]
        public int SecAttributeMin { get; set; }
        
        [Name("SecondaryAttributeMax")]
        public int SecAttributeMax { get; set; }
        
        [Name("SkillMin")]
        public int SkillMin { get; set; }
        
        [Name("SkillMax")]
        public int SkillMax { get; set; }
        
        [Name("WorkEthicMin")]
        public int WE { get; set; }
        
        [Name("EnduranceMin")]
        public int End { get; set; }

        [Name("CompetitivenessMin")]
        public int Comp { get; set; }

        [Name("AllowPositionalRandomTag")]
        public bool AllowPositionalTag { get; set; }

        [Name("AllowPersonalityRandomTag")]
        public bool AllowPersonalityTag { get; set; }

        [Name("AllowNegativeTrait")]
        public bool AllowNegativeTrait { get; set; } = false;


    }
}
